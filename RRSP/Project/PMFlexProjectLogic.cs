using Meros.Project;
using Meros.Risk;
using Meros.Stakeholder;
using Meros.Tasks;
using Signum.Alerts;
using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;
using System.ComponentModel.Design.Serialization;


namespace RRSP.Project;

public static class RRSPProjectLogic
{
    [AutoExpressionField]
    public static bool HasOpenTask(this StakeholderEntity entity) => 
        As.Expression(() => Database.Query<TaskEntity>()
            .Any(t => (t.State == TaskState.Open || t.State == TaskState.InProgress) && 
            t.Mixin<TaskStakeholderMixin>().Stakeholders.Any(ts => ts.Is(entity))));

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;
        {
            if (sb.WebServerBuilder != null)
            {
                ReflectionServer.RegisterLike(typeof(NewProjectModel), () => TypeAuthLogic.GetAllowed(typeof(ProjectEntity)).MaxUI() >= TypeAllowedBasic.Read);                
            }

            QueryLogic.Expressions.Register((StakeholderEntity s) => s.HasOpenTask());

            new Graph<ProjectEntity, DomainState>.Construct(ProjectExpandedOperation.Create)
            {
                ToStates = { DomainState.Active },
                Construct = (args) =>
                {
                    var model = args.GetArg<NewProjectModel>();
                    return CreateProjectFromModel(model);
                },
            }.Register();

            new Graph<ProjectEntity, DomainState>.Execute(ProjectExpandedOperation.Split)
            {
                FromStates = { DomainState.Active },
                ToStates = { DomainState.Active },
                Execute = (e, args) =>
                {
                    var splitModel = args.GetArg<ProjectSplitModel>();

                    var columns = ProjectSplitLogic.SplitColumns(splitModel);

                    var labels = ProjectSplitLogic.SplitLabels(splitModel);

                    var sourceProj = splitModel.SourceProject.Retrieve();
                    var newTypes = splitModel.Labels.Where(a => a.Action == LabelAction.Move || a.Action == LabelAction.Copy).Select(a => a.Label.Retrieve().Type).NotNull().Distinct().Where(t => sourceProj.Mixin<DomainTaskMixin>().LabelTypes.Contains(t)).ToList();
                    var targetProj = splitModel.TargetProject.Retrieve();
                    targetProj.Mixin<DomainTaskMixin>().LabelTypes.AddRange(newTypes.Where(nt => targetProj.Mixin<DomainTaskMixin>().LabelTypes.Contains(nt)));
                    targetProj.Execute(ProjectOperation.Save);

                    ProjectSplitLogic.SplitTasks(splitModel, labels, columns);
                },
            }.Register();
        }

    }

    private static ProjectEntity CreateProjectFromModel(NewProjectModel model)
    {       
        var newProject = new ProjectEntity
        {
            Name = model.ProjectName,
            Manager = model.ProjectManager,
            Phase = ProjectPhase.Initiating,            
        };

        newProject.Mixin<DomainTaskMixin>().Prefix = model.ProjectPrefix;
        newProject.Mixin<DomainRiskMixin>().RiskManagement = model.RiskManagement;
        newProject.Execute(ProjectOperation.Save);


        var members = new List<MemberEntity>();
        if (newProject.Manager != null)
        {
            members.Add(new MemberEntity
            {
                Domain = newProject.ToLite(),
                User = newProject.Manager,
                Role = ProjectLogic.DefaultPMRole!,
            });
        }

        if (newProject.Manager?.Is(UserEntity.Current) == false)
        {
            members.Add(new MemberEntity
            {
                Domain = newProject.ToLite(),
                User = UserEntity.Current,
                Role = ProjectLogic.DefaultPMRole!,
            });
        }

        members.AddRange(model.Members.Where(a => !a.Is(model.ProjectManager) && !a.Is(UserEntity.Current)).Select(m => new MemberEntity
        {
            Domain = newProject.ToLite(),
            User = m,
            Role = ProjectLogic.DefaultMemberRole!,
        }));

        members.BulkInsert();

        var users = model.Members.Where(a => !a.Is(UserEntity.Current)).ToList();
        foreach (var rem in users)
        {
            newProject.CreateAlert(ProjectAlert.AddedAsMemberOfProject, recipient: rem, groupTarget: newProject.ToLite());
        }

        if (model.CreateNewBoard != null)
        {
            var columns = model.CreateNewBoard.Columns.Select(c => new ColumnEntity
            {
                Name = c.Name,
                ArchiveTasksOlderThan = c.ArchiveTasksOlderThan,
                TaskState = c.TaskState
            }).ToList();

            new BoardEntity
            {
                Name = "Board",
                Domain = newProject.ToLite(),
                Columns = columns.ToMList(),
            }.Execute(BoardOperation.Save);
        }

        foreach (var item in model.CreateTasksFromLessonsLearned.RetrieveList())
        {
            new TaskEntity
            {
                Domain = newProject.ToLite(),
                Title = item.Title,
                Description = item.Description,
            }.Execute(TaskOperation.Save);
        }

        return newProject;
    }
}
