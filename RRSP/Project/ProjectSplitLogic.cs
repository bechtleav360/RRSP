using Meros.Project;
using Meros.Project.Orders;
using Meros.Tasks;
using Microsoft.SqlServer.Types;
using Signum.Utilities;

namespace RRSP.Project;

public static class ProjectSplitLogic
{

    public static void SplitTasks(ProjectSplitModel model, Dictionary<TaskLabelEntity, TaskLabelEntity?> labels, Dictionary<ColumnEntity, ColumnEntity?> columns)
    {
        var routes = model.Tasks.Select(a => a.Task).ToList()
            .Chunk(Schema.Current.Settings.MaxNumberOfParameters)
            .SelectMany(list => Database.Query<TaskEntity>().Where(t => list.Contains(t.ToLite())).Select(t => KeyValuePair.Create(t.Route, t.ToLite())).ToList())
            .ToDictionaryEx();

        var tree = TreeHelper.ToTreeS(routes.Keys, a => a.GetLevel() == 1 ? null : routes.ContainsKey(a.GetAncestor(1)) ? a.GetAncestor(1) : null);

        var lastId = TaskLogic.LastChild(SqlHierarchyId.GetRoot(), model.TargetProject);

        var mapping = tree.ToDictionary(a => a.Value, a => lastId = SqlHierarchyId.GetRoot().GetDescendant(lastId, SqlHierarchyId.Null));

        foreach (var taskAction in model.Tasks)
        {
            var task = taskAction.Task.RetrieveAndRemember();
            var map = mapping.FirstEx(a => (bool)task.Route.IsDescendantOf(a.Key));
            task.Route = task.Route.GetReparentedValue(map.Key, map.Value);
            task.Domain = model.TargetProject;
            task.ProjectPrefix = model.TargetProject.InDB(a => a.Mixin<DomainTaskMixin>().Prefix);
            task.Labels = task.Labels.Select(l => labels.GetOrThrow(l)).NotNull().ToMList();
            task.Column = task.Column == null ? null : columns.GetOrThrow(task.Column);
            task.State = task.Column?.InDB(c => c.TaskState) ?? TaskState.Open;
            if (task.Column != null)
            {
                var maxOrder = Database.Query<TaskEntity>().Where(t => t.Column.Is(task.Column)).Select(t => t.Order).Max();
                task.Order = (maxOrder ?? -1) + 1;
            }

            task.Mixin<TaskStakeholderMixin>().Stakeholders.Clear();
            task.Mixin<TaskRiskMixin>().Risks.Clear();

            task.Execute(TaskOperation.Save);
        }

        var removed = Database.Query<TaskRelationshipEntity>().Where(a => a.FromTask.Entity.Domain.Is(model.SourceProject) && a.ToTask.Entity.Domain.Is(model.TargetProject)).UnsafeDelete();
        var removed2 = Database.Query<TaskRelationshipEntity>().Where(a => a.FromTask.Entity.Domain.Is(model.TargetProject) && a.ToTask.Entity.Domain.Is(model.SourceProject)).UnsafeDelete();
    }


    public static Dictionary<TaskLabelEntity, TaskLabelEntity?> SplitLabels(ProjectSplitModel model)
    {
        Dictionary<TaskLabelEntity, TaskLabelEntity?> labelTransitions = new Dictionary<TaskLabelEntity, TaskLabelEntity?>();


        foreach (var labelAction in model.Labels)
        {
            switch (labelAction.Action)
            {
                case LabelAction.Move:
                    var label = labelAction.Label.Retrieve();
                    label.Domain = model.TargetProject;
                    label.Execute(TaskLabelOperation.Save);
                    labelTransitions.Add(label, label);
                    break;
                case LabelAction.Copy:

                    var oldLabel = labelAction.Label.Retrieve();

                    var newLabel = new TaskLabelEntity()
                    {
                        Name = oldLabel.Name,
                        Color = oldLabel.Color,
                        State = oldLabel.State,
                        Type = oldLabel.Type,
                        Domain = model.TargetProject,
                    }.Execute(TaskLabelOperation.Save);

                    labelTransitions.Add(oldLabel, newLabel);

                    break;
                case LabelAction.Replace:
                    labelTransitions.Add(labelAction.Label.Retrieve(), labelAction.TargetLabel!.Retrieve());
                    break;
                case LabelAction.Ignore:
                    labelTransitions.Add(labelAction.Label.Retrieve(), null);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return labelTransitions;
    }

    public static Dictionary<ColumnEntity, ColumnEntity?> SplitColumns(ProjectSplitModel model)
    {
        Dictionary<ColumnEntity, ColumnEntity?> columnTransitions = new Dictionary<ColumnEntity, ColumnEntity?>();

        List<ColumnEntity> columnsToSave = new List<ColumnEntity>();

        foreach (var columnAction in model.Columns)
        {
            switch (columnAction.Action)
            {
                case ColumnAction.Replace:
                    columnTransitions.Add(columnAction.Column.Retrieve(), columnAction.TargetColumn!.Retrieve());
                    break;

                case ColumnAction.Ignore:
                    columnTransitions.Add(columnAction.Column.Retrieve(), null);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        columnsToSave.SaveList();

        return columnTransitions;
    }

    private static List<Lite<TaskEntity>> TaskAndChildTasks(List<Lite<TaskEntity>> tasks)
    {
        List<Lite<TaskEntity>> allChildTasks = new List<Lite<TaskEntity>>();

        foreach (var task in tasks)
        {
            var relChildTasks = Database.Query<TaskRelationshipEntity>().Where(tr => tr.FromTask.Is(task) && tr.Type == TaskRelationshipType.Child).Select(tr => tr.ToTask).ToList();

            allChildTasks.AddRange(relChildTasks.Where(l2 => allChildTasks.All(l1 => l2.Is(l1) == false)));
        }

        if (allChildTasks.Count > 0)
        {
            var childTasks = TaskAndChildTasks(allChildTasks);

            allChildTasks.AddRange(childTasks.Where(l2 => allChildTasks.All(l1 => l2.Is(l1) == false)));
        }

        tasks.AddRange(allChildTasks.Where(l2 => tasks.All(l1 => l2.Is(l1) == false)));

        return tasks;
    }
}

