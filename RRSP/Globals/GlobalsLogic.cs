using Meros.Project;
using Signum.Authorization.Rules;
using Signum.Authorization;
using Signum.Word;
using Signum.API;
using Meros.Project.Portfolio;
namespace RRSP.Globals;

public static class GlobalsLogic
{
    [AutoExpressionField]
    public static int ActiveProjects(this RessortEntity r) => 
        As.Expression(() => Database.Query<ProjectEntity>().Count(prj => prj.State == DomainState.Active && prj.Manager != null && prj.Manager.Entity.Mixin<UserMixin>().Ressort.Is(r)));

    [AutoExpressionField]
    public static int ActiveProjects(this ReferatEntity r) =>
        As.Expression(() => Database.Query<ProjectEntity>().Count(prj => prj.State == DomainState.Active && prj.Manager != null && prj.Manager.Entity.Mixin<UserMixin>().Referat.Is(r)));

    [AutoExpressionField]
    public static IQueryable<PortfolioEntity> Portfolios(this RessortEntity r) =>
        As.Expression(() => Database.Query<PortfolioEntity>().Where(p => p.AllProjects().Any(prj => prj.Manager != null && prj.Manager.Entity.Mixin<UserMixin>().Ressort.Is(r))));

    [AutoExpressionField]
    public static IQueryable<PortfolioEntity> Portfolios(this ReferatEntity r) =>
        As.Expression(() => Database.Query<PortfolioEntity>().Where(p => p.AllProjects().Any(prj => prj.Manager != null && prj.Manager.Entity.Mixin<UserMixin>().Referat.Is(r))));

    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<ApplicationConfigurationEntity>()
            .WithSave(ApplicationConfigurationOperation.Save)
            .WithQuery(() => s => new
            {
                Entity = s,
                s.Id,
                Active = s.Is(Starter.Configuration.Value),
                s.Environment,
                s.Email.SendEmails,
                s.Email.OverrideEmailAddress,
                s.Email.DefaultCulture,
                s.Email.UrlLeft
            });

        sb.Include<RessortEntity>()
            .WithSave(RessortOperation.Save)
            .WithDelete(RessortOperation.Delete)
            .WithExpressionTo(r => r.Portfolios())
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name
            });

        QueryLogic.Expressions.Register((RessortEntity r) => r.ActiveProjects(), GlobalMessage.ActiveProjects);

        sb.Include<ReferatEntity>()
            .WithSave(ReferatOperation.Save)
            .WithDelete(ReferatOperation.Delete)
            .WithExpressionTo(r => r.Portfolios())
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name
            });

        QueryLogic.Expressions.Register((ReferatEntity r) => r.ActiveProjects(), GlobalMessage.ActiveProjects);

        Starter.Configuration = sb.GlobalLazy(
            () => Database.Query<ApplicationConfigurationEntity>().Single(a => a.DatabaseName == Connector.Current.OriginalDatabaseName()),
            new InvalidateWith(typeof(ApplicationConfigurationEntity)));

        if (sb.WebServerBuilder != null)
        {
            ReflectionServer.RegisterLike(typeof(WordTemplateExpandedOperation), () => TypeAuthLogic.GetAllowed(typeof(WordTemplateEntity)).MaxUI() >= TypeAllowedBasic.Write);
            ReflectionServer.RegisterLike(typeof(GlobalMessage), () => TypeAuthLogic.GetAllowed(typeof(ProjectEntity)).MaxUI() >= TypeAllowedBasic.Read, overrideRegistration: true);
        }

        new Graph<UserEntity>.Delete(UserExpandedOperation.DeleteWithAlternative)
        {
            Delete = (toDelete, args) =>
            {
                var alternative = args.GetArg<Lite<UserEntity>>();

                Database.Query<MemberEntity>()
                .Where(pm => pm.User.Is(toDelete) && pm.Domain.Entity.Members().Any(pm => pm.User.Is(alternative)))
                .UnsafeDelete();

                Administrator.MoveAllForeignKeys(toDelete.ToLite(), alternative);

                toDelete.Delete();
            },
        }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();

        new Graph<WordTemplateEntity>.ConstructFrom<WordTemplateEntity>(WordTemplateExpandedOperation.Clone)
        {
            Construct = (template, args) =>
            {
                return new WordTemplateEntity 
                { 
                    Name = "Clone of " + template.Name,
                    Query = template.Query,
                    Model = template.Model,
                    Culture = template.Culture,
                    GroupResults = template.GroupResults,
                    FileName = template.FileName,
                    Filters = template.Filters.ToMList(),
                    Orders = template.Orders.ToMList(),
                    Applicable = template.Applicable,
                    DisableAuthorization = template.DisableAuthorization,
                    Template = template.Template,
                    WordTransformer = template.WordTransformer,
                    WordConverter = template.WordConverter,
                };
            },
        }.Register();
    }
}
