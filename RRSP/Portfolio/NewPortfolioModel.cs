using Meros.Project;
using Meros.Project.Portfolio;
using Meros.Risk;
using Meros.Tasks;
using Signum.Authorization;

namespace RRSP.Portfolio;

public class NewPortfolioModel : ModelEntity
{
    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }

    public PortfolioType Type { get; set; }

    public Lite<UserEntity> Responsible { get; set; }

    [StringLengthValidator(Max = 10)]
    public string PortfolioPrefix { get; set; }

    public RiskManagementEmbedded? RiskManagement { get; set; }
}

[AutoInit]
public static class PortfolioExpandedOperation
{
    public static readonly ConstructSymbol<PortfolioEntity>.Simple Create;
}
