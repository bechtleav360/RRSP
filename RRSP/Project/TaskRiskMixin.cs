using Meros.Risk;
using Meros.Stakeholder;

namespace RRSP.Project;
public class TaskRiskMixin : MixinEntity
{
    TaskRiskMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    [NoRepeatValidator]
    public MList<Lite<RiskEntity>> Risks { get; set; } = new MList<Lite<RiskEntity>>();
}
