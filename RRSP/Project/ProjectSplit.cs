using Meros.Project;
using Meros.Project.Orders;
using Meros.Tasks;

namespace RRSP.Project;

public class ProjectSplitModel : ModelEntity
{
    public Lite<ProjectEntity> SourceProject { get; set; }

    public Lite<ProjectEntity> TargetProject { get; set; }

    public MList<TaskActionEmbedded> Tasks { get; set; } = new MList<TaskActionEmbedded>();

    public MList<LabelActionEmbedded> Labels { get; set; } = new MList<LabelActionEmbedded>();

    public MList<ColumnActionEmbedded> Columns { get; set; } = new MList<ColumnActionEmbedded>();

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Tasks))
            return NoRepeatValidatorAttribute.ByKey(Tasks, a => a.Task);

        if (pi.Name == nameof(Labels))
            return NoRepeatValidatorAttribute.ByKey(Labels, a => a.Label);

        if (pi.Name == nameof(Columns))
            return NoRepeatValidatorAttribute.ByKey(Columns, a => a.Column);

        if (pi.Name == nameof(TargetProject) && TargetProject.Is(SourceProject))
            return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.DistinctTo.NiceToString(), SourceProject);

        return base.PropertyValidation(pi);
    }
}

public class TaskActionEmbedded : EmbeddedEntity
{
    public Lite<TaskEntity> Task { get; set; }
}

public class LabelActionEmbedded : EmbeddedEntity
{
    public Lite<TaskLabelEntity> Label { get; set; }

    public LabelAction Action { get; set; }

    public Lite<TaskLabelEntity>? TargetLabel { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(TargetLabel))
            return (pi, TargetLabel).IsSetOnlyWhen(Action == LabelAction.Replace);

        return base.PropertyValidation(pi);
    }
}

public class ColumnActionEmbedded : EmbeddedEntity
{
    public Lite<ColumnEntity> Column { get; set; }

    public ColumnAction Action { get; set; }

    public Lite<ColumnEntity>? TargetColumn { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(TargetColumn))
            return (pi, TargetColumn).IsSetOnlyWhen(Action == ColumnAction.Replace);

        return base.PropertyValidation(pi);
    }
}



public enum LabelAction
{
    Move,
    Copy,
    Replace,
    Ignore,
}

public enum ColumnAction
{
    Move,
    Copy,
    Replace,
    Ignore,
}
