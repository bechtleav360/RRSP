using Meros.Stakeholder;

namespace RRSP.Project;
public class TaskStakeholderMixin : MixinEntity
{
    TaskStakeholderMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    [NoRepeatValidator]
    public MList<Lite<StakeholderEntity>> Stakeholders { get; set; } = new MList<Lite<StakeholderEntity>>();
}
