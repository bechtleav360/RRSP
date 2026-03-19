using Meros.PortfolioExt.LessonsLearned;
using Meros.Project;
using Meros.Project.Orders;
using Meros.Risk;
using Meros.Tasks;
using Signum.Authorization;
using Signum.Utilities.Reflection;

namespace RRSP.Project;

public class NewProjectModel : ModelEntity
{
    [StringLengthValidator(Min = 3, Max = 100), XmlStringValidator]
    public string ProjectName { get; set; }

    public Lite<UserEntity> ProjectManager { get; set; }

    [StringLengthValidator(Max = 10), XmlStringValidator]
    public string ProjectPrefix { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<Lite<UserEntity>> Members { get; set; } = [];

    public NewBoardEmbedded? CreateNewBoard { get; set; }

    public RiskManagementEmbedded? RiskManagement { get; set; }

    [PreserveOrder, NoRepeatValidator]
    public MList<Lite<LessonsLearnedEntity>> CreateTasksFromLessonsLearned { get; set; } = [];
}

public class NewBoardEmbedded : EmbeddedEntity
{
    [PreserveOrder, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 0)]
    public MList<NewColumnEmbedded> Columns { get; set; } = new MList<NewColumnEmbedded>();
}

public class NewColumnEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Min = 2, Max = 100), XmlStringValidator]
    public string Name { get; set; }

    public TaskState TaskState { get; set; }

    [Unit("days"), Description("Archive (done) tasks older than")]
    public int? ArchiveTasksOlderThan { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(TaskState))
        {
            if (TaskState == TaskState.ArchivedDone)
                return ValidationMessage._0IsNotAllowed.NiceToString(TaskState.ArchivedDone.NiceToString());
            else if (TaskState == TaskState.Rejected)
                return ValidationMessage._0IsNotAllowed.NiceToString(TaskState.Rejected.NiceToString());
        }

        return base.PropertyValidation(pi);
    }

}

[AutoInit]
public static class ProjectExpandedOperation
{
    public static readonly ConstructSymbol<ProjectEntity>.Simple Create;
    public static readonly ExecuteSymbol<ProjectEntity> Split;
}

public enum ProjectExtensionMessage
{
    Basic,
    Options
}
