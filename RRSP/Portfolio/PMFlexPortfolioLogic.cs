using Meros.PlanningProject.PSC;
using Meros.Project;
using Meros.Project.Portfolio;
using Meros.Risk;
using Meros.Tasks;
using RRSP.Globals;
using Signum.API;
using Signum.Authorization;
using Signum.Authorization.Rules;

namespace RRSP.Portfolio;

public static class RRSPPortfolioLogic
{
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        if (sb.WebServerBuilder != null)
        {
            ReflectionServer.RegisterLike(typeof(NewPortfolioModel), () => TypeAuthLogic.GetAllowed(typeof(PortfolioEntity)).MaxUI() >= TypeAllowedBasic.Read);
        }

        new Graph<PortfolioEntity, DomainState>.Construct(PortfolioExpandedOperation.Create)
        {
            ToStates = { DomainState.Active },
            Construct = (args) =>
            {
                var model = args.GetArg<NewPortfolioModel>();
                return CreatePortfolioFromModel(model);
            },
        }.Register();
    }

    private static PortfolioEntity CreatePortfolioFromModel(NewPortfolioModel model)
    {
        var role = Starter.Configuration.Value.DefaultPortfolioMangerRole ?? throw new InvalidOperationException(GlobalMessage.DefaultPortfolioManagerRoleIsNotConfigured.NiceToString());
        
        var newPortfolio = new PortfolioEntity
        {
            Name = model.Name,
            Type = model.Type,
            State = DomainState.Active,
            Responsible = model.Responsible,
        };

        newPortfolio.Mixin<DomainTaskMixin>().Prefix = model.PortfolioPrefix;
        newPortfolio.Mixin<DomainRiskMixin>().RiskManagement = model.RiskManagement;
        newPortfolio.Execute(PortfolioOperation.Save);

        new MemberEntity
        {
            Domain = newPortfolio.ToLite(),
            User = newPortfolio.Responsible,
            Role = role,
        }.Execute(MemberOperation.Save);

        if (newPortfolio.Responsible.Is(UserEntity.Current) == false)
        {
            new MemberEntity
            {
                Domain = newPortfolio.ToLite(),
                User = UserEntity.Current,
                Role = role,
            }.Execute(MemberOperation.Save);
        }

        return newPortfolio;
    }
}
