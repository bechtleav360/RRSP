using RRSP.Globals;
using Meros.PortfolioExt;
using Meros.Project;
using Meros.Project.Customers;
using Meros.Project.Orders;
using Meros.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Types;
using Signum.Authorization;
using Signum.Basics;
using Signum.CodeGeneration;
using Signum.DiffLog;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Signum.Files;
using Signum.Help;
using Signum.Mailing;
using Signum.Migrations;
using Signum.Security;
using Signum.Translation;
using Signum.UserAssets;
using Signum.ViewLog;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Meros.PlanningProject.Base;
using Meros.PlanningProject.WorkPackage;
using Meros.Protocol;
using Meros.StatusReport;
using Meros.Stakeholder;
using Meros.PlanningProject.PSC;
using Meros.Risk;
using Meros.PlanningProject.BusinessCase;
using Meros.PlanningProject.RASCI;
using Meros.PortfolioExt.InitiationRequest;

namespace RRSP.Terminal;

class Program
{
    public static IConfigurationRoot ConfigRoot = null!;

    static int Main(string[] args)
    {
        Dictionary<string, string> replacements = new Dictionary<string, string>()
        {
            { "project_", "" },
            { "Project", "" },
            //{ "pm_domain", "domain_level" },
            //{ "project_member", "member" },
            //{ "portfolio.", "project."},
            //{ "specific_domain", "only_for"},
            //{ "project_id_", "domain_id_"},
            //{ "project_id", "domain_id_project"},
            
            //{ "domain_id", "billing_id"},
        };
        
        Replacements.GlobalAutoReplacement = c => {

            var replaced = c.OldValue.Replace(replacements);

            var newValue = c.NewValues?.Where(a => a == replaced).Only();

            if (newValue != null)
                return new Replacements.Selection(c.OldValue, newValue);

            return null;
        };
        
        CRLFChecker.CheckGitCRLF();
        try
        {

            using (AuthLogic.Disable())
            using (CultureInfoUtils.ChangeCulture("en"))
            using (CultureInfoUtils.ChangeCultureUI("en"))
            {
                ConfigRoot = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                    .AddUserSecrets<Program>(optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                Console.WriteLine();

                var connectionString = ConfigRoot.GetConnectionString("ConnectionString")!;
                var isPostgres = ConfigRoot.GetValue<bool>("IsPostgres")!;
                Starter.OverrideAutoExternal = ConfigRoot.GetValue<string>("OverrideAutoExternal");

                Starter.Start(
                    connectionString,
                    isPostgres,
                    ConfigRoot.GetConnectionString("AzureStorageConnectionString"),
                    ConfigRoot.GetValue<string>("BroadcastSecret"), 
                    ConfigRoot.GetValue<string>("BroadcastUrls"),
                    wsb: null,
                    ConfigRoot.GetValue<string>("FilesRoot")
                );

                Console.WriteLine("..:: Welcome to RRSP Loading Application ::..");
                var color = !Connector.Current.DataSourceName().Contains("database.windows.net") ? ConsoleColor.Gray :
                    (Connector.Current.DatabaseName().Contains("test") ? ConsoleColor.Yellow : ConsoleColor.Red);

                SafeConsole.WriteLineColor(color, $"{Connector.Current}");
                Console.WriteLine();

                var MigrationDir = ConfigRoot.GetValue<string>("MigrationDir")!;
                SqlMigrationRunner.MigrationsDirectory = MigrationDir.DefaultText(@"..\..\..\Migrations");

                if (args.Any())
                {
                    switch (args.First().ToLower().Trim('-', '/'))
                    {
                        case "sql": 
                            SqlMigrationRunner.SqlMigrations(true); 
                            return 0;
                        case "csharp":
                            RRSPMigrations.CSharpMigrations(true);
                            return 0;
                        case "ar":
                            Schema.Current.Initialize();
                            AuthLogic.ImportAuthRules(XDocument.Load(@"AuthRules.xml"), interactive: false);
                            return 0;
                        case "ua":
                            Schema.Current.Initialize();
                            using(UserHolder.UserSession(AuthLogic.SystemUser!))
                                UserAssetsImporter.ImportAll(File.ReadAllBytes(@"UserAssets.xml"));
                            return 0;

                        case "load": Load(args.Skip(1).ToArray()); return 0;
                        default:
                            {
                                SafeConsole.WriteLineColor(ConsoleColor.Red, "Unkwnown command " + args.First());
                                Console.WriteLine("Examples:");
                                Console.WriteLine("   sql: SQL Migrations");
                                Console.WriteLine("   csharp: C# Migrations");
                                Console.WriteLine("   load 1-4,7: Load processes 1 to 4 and 7");
                                return -1;
                            }
                    }
                } //if(args.Any())

                while (true)
                {
                    Action? action = new ConsoleSwitch<string, Action>
                    {
                        {"CDB", ()=> Administrator.CleanDatabase(), "CleanDatabase"},
                        {"N", Administrator.NewDatabase},
                        {"MTP", () => SqlServerToPostgresMigration.MigrateToPostgres(ConfigRoot.GetConnectionString("MigrateToConnectionString")!, new MigrateToPostgresOptions
                        {
                            CleanDataTable = (dt, diffTable) =>
                            {
                                if(diffTable.Name.Name == "Exception")
                                {
                                    foreach (var row in dt.Rows.Cast<DataRow>())
                                    {
                                        if(row["ToStr"] is string s && s.Contains("ServiceUnavailable"))
                                            row["ToStr"] = s.Before("ServiceUnavailable") + "ServiceUnavailable";
                                        if(row["ExceptionMessage"] is string em && em.Contains("ServiceUnavailable"))
                                            row["ExceptionMessage"] = em.Before("ServiceUnavailable") + "ServiceUnavailable";
                                    }
                                }

                            }
                        }), "Migrate To Postgres" },
                        {"fixID", SqlServerToPostgresMigration.UpdateIdentities},
                        {"G", CodeGenerator.GenerateCodeConsole },
                        {"SQL", SqlMigrationRunner.SqlMigrations},
                        {"CS", () =>  { RRSPMigrations.CSharpMigrations(false); }, "C# Migrations"},
                        {"S", Administrator.Synchronize},
                        {"L", () => Load(null), "Load"},
                        {"CT", TranslationLogic.CopyTranslations},
                    }.Choose();

                    if (action == null)
                        return 0;

                    action();
                }
            }
        }
        catch (Exception e)
        {
            SafeConsole.WriteColor(ConsoleColor.DarkRed, e.GetType().Name + ": ");
            SafeConsole.WriteLineColor(ConsoleColor.Red, e.Message);
            SafeConsole.WriteLineColor(ConsoleColor.DarkRed, e.StackTrace!.Indent(4));
            return -1;
        }
    }

    private static void Load(string[]? args)
    {
        Schema.Current.Initialize();

        OperationLogic.AllowSaveGlobally = true;

        while (true)
        {
            var actions = new ConsoleSwitch<string, Action>
            {
                {"AR", AuthLogic.ImportExportAuthRules},
                {"ETR", FixTaskRelationships},
                {"HL", HelpExportImport.ImportExportHelpMenu},
                {"CleanRRSP", CleanRRSPProjects },
            }.ChooseMultipleWithDescription(args);

            if (actions == null)
                return;

            foreach (var acc in actions)
            {
                MigrationLogic.ExecuteLoadProcess(acc.Value, acc.Description);
            }
        }
    }

    static void FixTaskRelationships()
    {
        var errors = Database.Query<TaskRelationshipEntity>()
           .Where(a => a.Type == TaskRelationshipType.Parent && (bool)(a.FromTask.Entity.Route.GetAncestor(1) != a.ToTask.Entity.Route))
           .Select(a => new
           {
               a.FromTask,
               FromProject = a.FromTask.Entity.Domain,
               FromRoute = a.FromTask.Entity.Route,

               a.ToTask,
               ToProject = a.ToTask.Entity.Domain,
               ToRoute = a.ToTask.Entity.Route,
           })
           .ToListWait("auto");

        var projects = errors.Select(a => a.FromProject).Distinct().ToList();

        foreach (var proj in projects)
        {
            Console.WriteLine(proj.ToString());
            FixProject(proj);
        }

        //var errors2 = Database.Query<TaskRelationshipEntity>().Where(a => a.Type == TaskRelationshipType.Child && (bool)(a.ToTask.Entity.Route.GetAncestor(1) != a.FromTask.Entity.Route)).ToListWait("auto");
    }

    static void CleanRRSPProjects()
    {
        var projects = Database.Query<ProjectEntity>().Where(a => a.Name.Contains("PMflexONE")).ToList();

        foreach (var proj in projects)
        {

            Database.MListQuery((ProjectPrioritizationEntity e) => e.Projects).Where(mle => mle.Element.Entity.FromDomain.Is(proj)).UnsafeDeleteMList();
            Database.Query<InitiationRequestEntity>().Where(a => a.FromDomain.Is(proj)).UnsafeDelete();

            Database.Query<NotificationConfigEntity>().Where(a => ((TaskEntity)a.Target.Entity).Domain.Is(proj)).UnsafeDelete();
            Database.Query<NotificationConfigEntity>().Where(a => ((ColumnEntity)a.Target.Entity).Board.Entity.Domain.Is(proj)).UnsafeDelete();
            Database.Query<TaskRelationshipEntity>().Where(a => a.FromTask.Entity.Domain.Is(proj) || a.ToTask.Entity.Domain.Is(proj)).UnsafeDelete();
            Database.Query<TaskEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<TaskLabelEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<ColumnEntity>().Where(a => a.Board.Entity.Domain.Is(proj)).UnsafeDelete();
            Database.Query<BoardEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<ProtocolPointEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<MeetingProtocolEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<StatusReportEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<CharterEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<RiskEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<TaskEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<ProjectWorkPackageEntity>().Where(a => a.ProjectPlan.Entity.Project.Is(proj)).UnsafeDelete();
            Database.Query<PlanningRoleEntity>().Where(a => a.Project.Is(proj)).UnsafeDelete();
            proj.InDB().UnsafeUpdate().Set(a => a.Mixin<ProjectPlanMixin>().DefaultPlan, a => null).Execute();
            Database.Query<ProjectPlanEntity>().Where(a => a.Project.Is(proj)).UnsafeDelete();
            Database.Query<EventEntity>().Where(a => a.Project.Is(proj)).UnsafeDelete();
            Database.Query<MilestoneEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<StakeholderLogEntity>().Where(a => a.Stakeholder.Entity.Domain.Is(proj)).UnsafeDelete();
            Database.Query<StakeholderEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<RequirementEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<GoalEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<BusinessCaseEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<DeliverableEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<AssumptionEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<ConstraintEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<ServiceEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<MemberEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<ExternalMemberEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<RASCIMatrixEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<RASCIRoleEntity>().Where(a => a.Domain.Is(proj)).UnsafeDelete();
            Database.Query<ProjectClassificationEntity>().Where(a => a.Project.Is(proj)).UnsafeDelete(); 
            proj.Delete();
        }
    }

    static void FixProject(Lite<IDomainEntity> project)
    {
        var parentDictionary = Database.Query<TaskRelationshipEntity>().Where(a => a.FromTask.Entity.Domain.Is(project) && a.ToTask.Entity.Domain.Is(project) && a.Type == TaskRelationshipType.Parent).ToDictionary(a => a.FromTask, a => a.ToTask);

        var routeInfoByTask = Database.Query<TaskEntity>().Where(a => a.Domain.Is(project)).Select(a => new CurrentRouteInfo { Lite = a.ToLite(), TaskNumber = a.TaskNumber, Route = a.Route }).ToDictionaryEx(a => a.Lite);

        var trees = TreeHelper.ToTreeC(routeInfoByTask.Values, t =>
        {

            var parentLite = parentDictionary.TryGetC(t.Lite);

            if (parentLite == null)
                return null;

            return routeInfoByTask.GetOrThrow(parentLite);
        });

        var maxChild = routeInfoByTask.Values.Select(a => a.Route).GroupAggregateToDictionary(a => a.GetAncestor(1), gr => gr.Max());

        SetHierarhyId(trees, SqlHierarchyId.GetRoot(), maxChild);

        var modified = routeInfoByTask.Values.Where(a => a.NewRoute != null).ToList();

        foreach (var item in modified)
        {
            Console.WriteLine(item.Lite + "move from " + item.Route + " to " + item.NewRoute);

            item.Lite.InDB().UnsafeUpdate().Set(a => a.Route, a => item.NewRoute!.Value).Execute();
        }

    }

    static void SetHierarhyId(ObservableCollection<Node<CurrentRouteInfo>> tree, SqlHierarchyId newParent, Dictionary<SqlHierarchyId, SqlHierarchyId> maxChild)
    {
        var max = maxChild.TryGetS(newParent) ?? SqlHierarchyId.Null;

        foreach (var node in tree)
        {
            var cri = node.Value;
            var oldRoute = cri.Route; //For debugging
            if (cri.Route.GetAncestor(1) != newParent)
            {
                var newRoute = newParent.GetDescendant(max, SqlHierarchyId.Null);
                cri.NewRoute = newRoute;
                max = newRoute;
            }

            SetHierarhyId(node.Children, cri.NewRoute ?? cri.Route, maxChild);
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    class CurrentRouteInfo
    {
        public Lite<TaskEntity> Lite { get; internal set; }
        public int TaskNumber { get; internal set; }
        public SqlHierarchyId Route { get; internal set; }
        public SqlHierarchyId? NewRoute { get; internal set; }

        public override string ToString() => $"{Route} -> {NewRoute}";

    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    



  

}
