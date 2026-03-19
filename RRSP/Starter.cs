using Azure.Core;
using Azure.Identity;
using Meros.Financials;
using Meros.PlanningProject.Base;
using Meros.PlanningProject.BusinessCase;
using Meros.PlanningProject.PSC;
using Meros.PlanningProject.RASCI;
using Meros.PlanningProject.TaskManagement;
using Meros.PlanningProject.WorkPackage;
using Meros.PortfolioExt;
using Meros.PortfolioExt.DomainLabel;
using Meros.PortfolioExt.InitiationRequest;
using Meros.PortfolioExt.LessonsLearned;
using Meros.PortfolioExt.MyPortfolios;
using Meros.PortfolioExt.MyPrograms;
using Meros.PortfolioExt.MyProjects;
using Meros.PortfolioExt.PortfolioRelationship;
using Meros.PortfolioExt.ProgramBusinessCase;
using Meros.PortfolioExt.ProgramCharter;
using Meros.PortfolioExt.Responsibilities;
using Meros.PortfolioExt.StatusReportExtra;
using Meros.Project;
using Meros.Project.Customers;
using Meros.Project.Orders;
using Meros.Project.Portfolio;
using Meros.Project.Program;
using Meros.Protocol;
using Meros.Risk;
using Meros.Stakeholder;
using Meros.StatementOfWork;
using Meros.StatusReport;
using Meros.Tasks;
using Meros.Videos;
using Microsoft.AspNetCore.Html;
using Microsoft.SqlServer.Types;
using Npgsql;
using RRSP.Globals;
using RRSP.Project;
using RRSP.Program;
using RRSP.Portfolio;
using RRSP.WordTemplates;
using Signum.Alerts;
using Signum.API;
using Signum.Authorization;
using Signum.Authorization.ResetPassword;
using Signum.Authorization.Rules;
using Signum.Authorization.SessionLog;
using Signum.Authorization.UserTicket;
using Signum.Cache;
using Signum.Cache.Broadcast;
using Signum.Calendar;
using Signum.Chart;
using Signum.Chart.UserChart;
using Signum.ConcurrentUser;
using Signum.Dashboard;
using Signum.DiffLog;
using Signum.DynamicQuery.Tokens;
using Signum.Entities.Reflection;
using Signum.Eval;
using Signum.Excel;
using Signum.Files;
using Signum.Files.AzureBlobs;
using Signum.Files.FileTypeAlgorithms;
using Signum.Help;
using Signum.Tour;
using Signum.Mailing;
using Signum.Mailing.MicrosoftGraph;
using Signum.Mailing.Package;
using Signum.Map;
using Signum.Migrations;
using Signum.Omnibox;
using Signum.Processes;
using Signum.Profiler;
using Signum.Scheduler;
using Signum.Templating;
using Signum.TimeMachine;
using Signum.Toolbar;
using Signum.Translation;
using Signum.Translation.Instances;
using Signum.Translation.Translators;
using Signum.UserQueries;
using Signum.WhatsNew;
using Signum.Word;
using System.Globalization;
using System.IO;
using Meros.Protocol.Decision;



namespace RRSP;

//Starts-up the engine for RRSP Entities, used by Web and Load Application
public static partial class Starter
{
    public static ResetLazy<ApplicationConfigurationEntity> Configuration = null!;

    public static string? AzureStorageConnectionString { get; private set; }
    
    public static string? FilesRoot { get; private set; }

    public static string? NavbarColor;

    public static string? OverrideAutoExternal;

    public static void Start(string connectionString, bool isPostgres, string? azureStorageConnectionString, string? broadcastSecret, string? broadcastUrls, WebServerBuilder? wsb, string? filesRoot   )
    {
        AzureStorageConnectionString = azureStorageConnectionString;
        FilesRoot = filesRoot;

        using (HeavyProfiler.Log("Start"))
        using (var initial = HeavyProfiler.Log("Initial"))
        {
            string? logDatabase = Connector.TryExtractDatabaseNameWithPostfix(ref connectionString, "_Log");

            SchemaBuilder sb = new CustomSchemaBuilder { LogDatabaseName = logDatabase, Tracer = initial, WebServerBuilder = wsb };
            if (!isPostgres)
            {
                var sqlVersion = SqlServerVersionDetector.Detect(connectionString, SqlServerVersion.SqlServer2017);
                Connector.Default = new SqlServerConnector(connectionString, sb.Schema, sqlVersion);
            }
            else
            {
                NpgsqlConnectionStringBuilder csb = new NpgsqlConnectionStringBuilder(connectionString);

                if (csb.Username == "autoExternal")
                {
                    sb.Schema.ExecuteAs = "azure_pg_admin";
                    csb.Username = OverrideAutoExternal ?? (Environment.UserName + "_bechtle.com#EXT#@bechtleavs.onmicrosoft.com");
                    connectionString = csb.ToString();
                }
                else if (csb.Username == "auto")
                {
                    sb.Schema.ExecuteAs = "azure_pg_admin";
                    csb.Username = Environment.UserName + "@bechtle.com";
                    connectionString = csb.ToString();
                }

                bool hasPassword = csb.Password.HasText();

                var postgreeVersion = PostgresVersionDetector.Detect(hasPassword ? connectionString :
                    (connectionString + ";Password=" + GetAuthToken(default).Result), null);

                Connector.Default = new PostgreSqlConnector(connectionString, sb.Schema, postgreeVersion, builder =>
                {
                    if (!hasPassword)
                        builder.UsePeriodicPasswordProvider((csb, token) => GetAuthToken(token), new TimeSpan(0, 10, 0), new TimeSpan(0, 0, 30));

                    builder.EnableTransportSecurity();
                    builder.EnableArrays();
                    builder.EnableLTree();
                    builder.EnableRanges();
                });
            }
            sb.Schema.Version = typeof(Starter).Assembly.GetName().Version!;
            sb.Schema.ForceCultureInfo = CultureInfo.GetCultureInfo("de-DE");
            sb.Schema.Settings.ImplementedByAllPrimaryKeyTypes.Add(typeof(Guid));

            MixinDeclarations.Register<RoleEntity, RoleMixin>();
            MixinDeclarations.Register<OperationLogEntity, DiffLogMixin>();
            MixinDeclarations.Register<EmailMessageEntity, EmailMessagePackageMixin>();

            MixinDeclarations.Register<UserEntity, UserMixin>();
            MixinDeclarations.Register<UserEntity, UserProjectMixin>();

            MixinDeclarations.Register<ProjectEntity, ProjectPortfolioMixin>();
            MixinDeclarations.Register<ProjectEntity, DomainTaskMixin>();
            MixinDeclarations.Register<ProjectEntity, DomainStakeholderMixin>();
            MixinDeclarations.Register<ProjectEntity, DomainProtocolMixin>();
            MixinDeclarations.Register<ProjectEntity, DomainStatusReportMixin>();
            MixinDeclarations.Register<ProjectEntity, DomainRiskMixin>();
            MixinDeclarations.Register<ProjectEntity, ProjectPlanMixin>();            

            MixinDeclarations.Register<ProgramEntity, DomainTaskMixin>();
            MixinDeclarations.Register<ProgramEntity, DomainStakeholderMixin>();
            MixinDeclarations.Register<ProgramEntity, DomainProtocolMixin>();
            MixinDeclarations.Register<ProgramEntity, DomainStatusReportMixin>();
            MixinDeclarations.Register<ProgramEntity, DomainRiskMixin>();

            MixinDeclarations.Register<PortfolioEntity, DomainProtocolMixin>();
            MixinDeclarations.Register<PortfolioEntity, DomainStatusReportMixin>();
            MixinDeclarations.Register<PortfolioEntity, DomainStakeholderMixin>();
            MixinDeclarations.Register<PortfolioEntity, DomainRiskMixin>();
            MixinDeclarations.Register<PortfolioEntity, DomainTaskMixin>();            

            MixinDeclarations.Register<TaskEntity, TaskStakeholderMixin>();
            MixinDeclarations.Register<TaskEntity, TaskRiskMixin>();

            MixinDeclarations.Register<DomainRoleEntity, DomainRoleStatusReportMixin>();

            MixinDeclarations.Register<MilestoneEntity, MilestoneRiskMixin>();
            
            MixinDeclarations.Register<ProtocolPointEntity, ProtocolRiskMixin>();

            MixinDeclarations.Register<ProtocolPointTypeEntity, ProtocolPointTypeRiskMixin>();
            MixinDeclarations.Register<ProtocolPointTypeEntity, ProtocolPointTypeTaskMixin>();
            MixinDeclarations.Register<ProtocolPointTypeEntity, ProtocolPointTypeDecisionMixin>();
            
            MixinDeclarations.Register<ProtocolPointDecisionEntity, ProtocolPointDecisionDecisionMixin>();

            MixinDeclarations.Register<UserQueryEntity, UserQueryMixin>();
            
            MixinDeclarations.Register<RiskEntity, RiskAssumptionMixin>();


            MixinDeclarations.Register<StatusReportEntity, StatusReportFinancialMixin>();
            As.ReplaceExpression((UserEntity u) => u.ToString(), u => $"{u.Mixin<UserProjectMixin>().FirstName} {u.Mixin<UserProjectMixin>().LastName}");

            EntityKindCache.Override(typeof(UserEntity), new EntityKindAttribute(EntityKind.Main, EntityData.Master));
            As.ReplaceExpression((UserEntity u) => u.EmailOwnerData, u => new EmailOwnerData
            {
                Owner = u.ToLite(),
                CultureInfo = u.CultureInfo,
                DisplayName = u.Mixin<UserProjectMixin>().LastNameFirstName,
                Email = u.Email,
                //AzureUserId = u.Mixin<UserAzureADMixin>().OID,
            });
            MixinDeclarations.Register<BigStringEmbedded, BigStringMixin>();

            ConfigureBigString(sb);
            OverrideAttributes(sb);

            Clock.Mode = TimeZoneMode.Utc;
            Schema.Current.TimeZoneMode = TimeZoneMode.Utc;


            sb.Settings.UdtSqlName.Add(typeof(SqlHierarchyId), "HierarchyId");

            //var postgreeVersion = wsb == null ? PostgresVersionDetector.Detect(connectionString) : null;
            //Connector.Default = new PostgreSqlConnector(connectionString, sb.Schema, postgreeVersion);


            if (wsb != null)
            {
                SignumServer.Start(wsb);

            }

            CacheLogic.Start(sb, withSqlDependency: false, serverBroadcast: 
                isPostgres ? new PostgresBroadcast():
                broadcastSecret != null && broadcastUrls != null ? new SimpleHttpBroadcast(broadcastSecret, broadcastUrls) : null);

            //Signum.Framework modules

            TypeLogic.Start(sb);

            OperationLogic.Start(sb);
            ExceptionLogic.Start(sb);
            VisualTipLogic.Start(sb);
            ChangeLogLogic.Start(sb);


            //Signum.Extensions modules

            MigrationLogic.Start(sb);

            CultureInfoLogic.Start(sb);
            FilePathEmbeddedLogic.Start(sb);
            BigStringLogic.Start(sb);
            EmailLogic.Start(sb, () => Configuration.Value.Email, (et, target, message) => Configuration.Value.EmailSender, Starter.GetFileTypeAlgorithm(f => f.AttachmentsFolder));
            MailingMicrosoftGraphLogic.Start(sb);
            
            AuthLogic.Start(sb, "System", null);
            
            UserEntity.ValidatePassword = RRSPPasswordValidation.ValidatePassword;
            
            //AzureADLogic.Start(sb, adGroupsAndQueries: false, deactivateUsersTask: true);
            //AzureADAuthenticationServer.ExtraValidAudiences = () => (Starter.Configuration.Value.ExtraValidAudiences ?? "")
            //.Split(",").Select(a => a.Trim()).Where(a => a.Length != 0);

            QueryLogic.Queries.Register(typeof(UserEntity), () =>
                from u in Database.Query<UserEntity>()
                select new
                {
                    Entity = u,
                    u.Id,
                    u.Mixin<UserProjectMixin>().FirstName,
                    u.Mixin<UserProjectMixin>().LastName,
                    u.UserName,
                    u.Email,
                    u.Role,
                    u.State,
                    u.CultureInfo,
                });

            OperationLogic.FindExecute(UserOperation.Save).OverrideExecute(baseCall => (e, args) =>
            {
                bool wasNew = e.IsNew;

                baseCall(e, args);

                AddUserToProjects(e, wasNew);
            });
         

            AuthLogic.StartAllModules(sb, () => Starter.Configuration.Value.AuthTokens);
            PermissionLogic.RegisterPermissions(LoginPermission.ChangePassword);
            
            //AuthLogic.Authorizer = new RRSPAuthorizer(adVariant => Configuration.Value.AzureAD);
            ResetPasswordRequestLogic.Start(sb);
            UserTicketLogic.Start(sb);
            SessionLogLogic.Start(sb);
            ProcessLogic.Start(sb);
            PackageLogic.Start(sb, packages: true, packageOperations: true);
            EmailPackageLogic.Start(sb);

            SchedulerLogic.Start(sb);
            CalendarDayLogic.Start(sb);
            QueryLogic.Start(sb);

            UserQueryLogic.Start(sb);
            UserQueryLogic.RegisterUserTypeCondition(sb, RRSPCondition.UserEntities);
            UserQueryLogic.RegisterRoleTypeCondition(sb, RRSPCondition.RoleEntities);

            ChartLogic.Start(sb, googleMapsChartScripts: false);
            OmniboxLogic.Start(sb);


            UserChartLogic.RegisterUserTypeCondition(sb, RRSPCondition.UserEntities);
            UserChartLogic.RegisterRoleTypeCondition(sb, RRSPCondition.RoleEntities);

            DashboardLogic.Start(sb, GetFileTypeAlgorithm(p => p.CachedQueryFolder));
            DashboardLogic.RegisterUserTypeCondition(sb, RRSPCondition.UserEntities);
            DashboardLogic.RegisterRoleTypeCondition(sb, RRSPCondition.RoleEntities);

            ToolbarLogic.Start(sb);
            ToolbarLogic.RegisterUserTypeCondition(sb, RRSPCondition.UserEntities);
            ToolbarLogic.RegisterRoleTypeCondition(sb, RRSPCondition.RoleEntities);

            DiffLogLogic.Start(sb, registerAll: true);
            SystemEventLogLogic.Start(sb);
            ExcelLogic.Start(sb, excelReport: true);

            FileLogic.Start(sb);
            AlertLogic.Start(sb, typeof(TaskEntity));
            AlertLogic.RegisterAlertNotificationMail(sb);
            
            TimeMachineLogic.Start(sb);

            TourLogic.Start(sb);
            WhatsNewLogic.Start(sb, GetFileTypeAlgorithm(p => p.WhatsNewPreviewFolder), GetFileTypeAlgorithm(p => p.WhatsNewDocumentFolder));
            WhatsNewLogic.RegisterPublishedTypeCondition(sb, RRSPCondition.Published);

            TranslationLogic.Start(sb, countLocalizationHits: false,
                        new AlreadyTranslatedTranslator(),
                        new AzureTranslator(
                            () => Configuration.Value.Translation.AzureCognitiveServicesAPIKey,
                            () => Configuration.Value.Translation.AzureCognitiveServicesRegion),
                        new DeepLTranslator(() => Configuration.Value.Translation.DeepLAPIKey)
                    );

            TranslationLogic.GetTargetDirectory = (System.IO.FileInfo fi, string cleanFileName, string appName, string rootDir) =>
            {
                if (cleanFileName.StartsWith("Meros."))
                    return $@"{rootDir}\Meros\{cleanFileName}\Translations";

                return TranslationLogic.GetTargetDirectoryDefault(fi, cleanFileName, appName, rootDir);
            };

            TranslatedInstanceLogic.Start(sb, () => CultureInfo.GetCultureInfo("de"));


            WordTemplateLogic.Start(sb);
            MapLogic.Start(sb);
            ProfilerLogic.Start(sb,
               timeTracker: true,
               heavyProfiler: true,
               overrideSessionTimeout: true);

            ConcurrentUserLogic.Start(sb, null);

            GlobalValueProvider.RegisterGlobalVariable("Signature", _ => {

                var Template = TextTemplateParser.Parse(
                                Configuration.Value.EmailSignature,
                                QueryLogic.Queries.QueryDescription(typeof(UserEntity)),
                                typeof(UserEntity));

                var user = UserEntity.Current.Retrieve();

                QueryDescription qd = QueryLogic.Queries.QueryDescription(typeof(UserEntity));

                List<QueryToken> tokens = new List<QueryToken>();

                Template.FillQueryTokens(tokens);

                var columns = tokens.Distinct().Select(qt => new Signum.DynamicQuery.Column(qt, null)).ToList();

                var filters = new List<Filter> { new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, user.ToLite()) };

                var table = QueryLogic.Queries.ExecuteQuery(new QueryRequest
                {
                    QueryName = qd.QueryName,
                    Columns = columns,
                    Pagination = new Pagination.All(),
                    Filters = filters,
                    Orders = [],
                });

                var columnTokens = table.Columns.ToDictionary(a => a.Token);

                string html = Template.Print(new TextTemplateParameters(user, CultureInfo.GetCultureInfo("de-DE"), new QueryContext(qd, table))
                {
                    IsHtml = true,
                });

                return new HtmlString(html);
            });

            HelpLogic.Start(sb, GetFileTypeAlgorithm(p => p.HelpImagesFolder));

            // Agile Modules
            ProjectLogic.Start(sb, GetFileTypeAlgorithm(f => f.ProjectChartFolder), 
                () => Configuration.Value.DefaultProjectManagerRole, () => Configuration.Value.DefaultProjectMemberRole, () => Configuration.Value.DefaultDashboard?.Project);

            TaskLabelLogic.Start(sb);
            BoardLogic.Start(sb);
            TaskLogic.Start(sb, () => Configuration.Value.Task);
            
            NotificationConfigLogic.Start(sb);
            AttachmentLogic.Start(sb, GetFileTypeAlgorithm(f => f.AttachmentsFolder));
            CommentLogic.Start(sb);

            // Billing Modules
            CustomerLogic.Start(sb);

            CustomerProjectLogic.Start(sb);

            //ProMT
            ProtocolLogic.Start(sb, 
                () => Configuration.Value.MeetingProtocolExcelTemplate,
                GetFileTypeAlgorithm(c => c.AttachmentsFolder), 
                GetFileTypeAlgorithm(c => c.ProtocolReportFolder),
                () => Configuration.Value.MeetingProtocolReportTemplate);
            
            DecisionLogic.Start(sb);

            StatusReportLogic.Start(sb, GetFileTypeAlgorithm(c => c.ProjectStatusReportFolder), () => Configuration.Value.StatusReportReportTemplate);

            BusinessCaseLogic.Start(sb,
                GetFileTypeAlgorithm(c => c.BusinessCaseBaseLineFolder), 
                () => Configuration.Value.BusinessCaseReportTemplate);
            BaseLogic.Start(sb);

            CharterLogic.Start(sb, () => Configuration.Value.ProjectCharterReportTemplate);    
            RasciMatrixLogic.Start(sb);

            PlanningGroupLogic.Start(sb);
            RiskLibraryLogic.Start(sb);
            RiskCategoryTableLogic.Start(sb);

            RiskLogic.Start(sb, 
                () => Configuration.Value.RiskManagementPercentReportTemplate, 
                () => Configuration.Value.RiskManagementReportTemplate);

            StakeholderLogic.Start(sb, () => Configuration.Value.ContactPersonReportTemplate);
            MilestoneLogic.Start(sb);
            EventLogic.Start(sb);

            PSCLogic.Start(sb, 
                GetFileTypeAlgorithm(c => c.ChangeRequestReportFolder),
                GetFileTypeAlgorithm(c => c.ChangeRequestAttachmentFolder), 
                () => Configuration.Value.ChangeRequestReportTemplate);
            WorkPackageLogic.Start(sb,
                GetFileTypeAlgorithm(c => c.WorkPackageReportFolder),
                GetFileTypeAlgorithm(c => c.WorkPackageAttachmentFolder), 
                () => Configuration.Value.WorkPackageReportTemplate);
            PlanLevelColorConfigLogic.Start(sb);
            MyProjectsLogic.Start(sb);
            MyProgramsLogic.Start(sb);
            MyPortfoliosLogic.Start(sb);
            FinancialsLogic.Start(sb);
            TaskManagementLogic.Start(sb);

            //Portfolio
            PortfolioLogic.Start(sb, () => Configuration.Value.DefaultDashboard?.Portfolio);
            ProgramLogic.Start(sb, () => Configuration.Value.DefaultDashboard?.Program);

            PortfolioExtLogic.Start(sb);
            InitiationRequestLogic.Start(sb);
            ProjectPrioritizationLogic.Start(sb);
            PortfolioRelationshipLogic.Start(sb);
            ProgramBusinessCaseLogic.Start(sb);
            ProgramCharterLogic.Start(sb);
            DomainResponsibilityLogic.Start(sb);
            LessonsLearnedLogic.Start(sb);
            DomainLabelLogic.Start(sb);
            StatusReportExtraLogic.Start(sb);

            QueryLogic.Queries.Register(typeof(PortfolioEntity), () => DynamicQueryCore.Auto(
                from e in Database.Query<PortfolioEntity>()
                select new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    Progress = new GoalProgresDTO
                    {
                        Progress = e.Goals().Average(a => a.Progress),
                    },
                    TotalRevenue = e.TotalRevenues(),
                    TotalAvailable = e.TotalAvailable(),
                    e.Responsible,
                    e.ParentPortfolio,
                    e.StartYear,
                    e.EndYear,
                })
                .ColumnDisplayName(a => a.TotalRevenue, () => FinancialMessage.TotalRevenue.NiceToString())
                .ColumnDisplayName(a => a.TotalAvailable, () => FinancialMessage.TotalAvailable.NiceToString())
                .ColumnDisplayName(a => a.Progress, () => PortfolioMessage.Progress.NiceToString())
                .Column(a => a.TotalRevenue, c => { c.Format = "K1"; c.Unit = "€"; })
                .Column(a => a.TotalAvailable, c => { c.Format = "K1"; c.Unit = "€"; }));

            //Videos
            VideoLogic.Start(sb, 
                GetFileTypeAlgorithm(c => c.VideosFolder, webDownload: true /* webPrefix: "~/videos"*/), 
                GetFileTypeAlgorithm(c => c.VideoThumbnailsFolder),
                GetFileTypeAlgorithm(c => c.VideoInlineImagesFolder));
            RRSPWordLogic.Start(sb);

            EvalLogic.AddFullAssembly(typeof(Entity));
            EvalLogic.AddFullAssembly(typeof(UserEntity));
            EvalLogic.AddFullAssembly(typeof(TaskEntity));

            //Statement of Work
            StatementOfWorkLogic.Start(sb, () => Configuration.Value.PerDayPrice, GetFileTypeAlgorithm(c => c.StatementOfWorkReportFolder));
            
            QueryLogic.Expressions.Register((RiskEntity e) => e.FirstOwner()).ForceImplementations = Implementations.By(typeof(ProjectEntity), typeof(ProgramEntity), typeof(PortfolioEntity));

            RRSPProjectLogic.Start(sb);
            RRSPProgramLogic.Start(sb);
            RRSPPortfolioLogic.Start(sb);
            GlobalsLogic.Start(sb);

            RegisterTypeConditions();

            Schema.Current.OnSchemaCompleted();
        }
    }

    public static async ValueTask<string> GetAuthToken(CancellationToken token)
    {
        //Problems to log-in in Terminal/Kestrel?
        //* Download and install Azure CLI and run "az login" in a command line

        //Problems to log-in in in IIS? 
        //* Download and install Azure CLI and run "az login" in a command line
        //* The application pool identity should be your identity.
        //* In %windir%\System32\inetsrv\config\applicationHost.config set setProfileEnvironment to true  (more info https://stackoverflow.com/a/67913578/38670)
        //* Reset iis

        var defaultAzure = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = true,
        });
        var tokenAsync = await defaultAzure.GetTokenAsync(new TokenRequestContext(scopes: new[] { "https://ossrdbms-aad.database.windows.net/.default" }), token);
        return tokenAsync.Token;
    }

    private static void RegisterTypeConditions()
    {
        // ==== AllowedRole
        TypeConditionLogic.RegisterCompile(RRSPCondition.AllowedRole, (UserEntity u) => u.Role.InDB(r => r.Mixin<RoleMixin>().AdministratedByBillingUser));
        TypeConditionLogic.RegisterCompile(RRSPCondition.AllowedRole, (VideoEntity v) => v.RestrictAccess == false || v.AllowedRoles.Contains(RoleEntity.Current));

        // ==== SOWEditor
        TypeConditionLogic.Register(RRSPCondition.SOWEditor, (StatementOfWorkEntity l) => l.Editors.Any(se => se.Is(UserEntity.Current)));
        TypeConditionLogic.Register(RRSPCondition.SOWEditor, (StatementOfWorkServiceEntity li) => li.Parent.Entity.InCondition(RRSPCondition.SOWEditor));

        // ===== CurrentUser
        TypeConditionLogic.RegisterCompile(RRSPCondition.CurrentUser, (WhatsNewLogEntity w) => w.User.Is(UserEntity.Current));
        TypeConditionLogic.RegisterCompile(RRSPCondition.CurrentUser, (UserEntity u) => u.Is(UserEntity.Current));
        TypeConditionLogic.Register(RRSPCondition.CurrentUser, (AlertEntity a) => a.Recipient.Is(UserEntity.Current) || a.CreatedBy.Is(UserEntity.Current));
        TypeConditionLogic.Register(RRSPCondition.CurrentUser, (NotificationConfigEntity bh) => bh.User.Is(UserEntity.Current));
        TypeConditionLogic.Register(RRSPCondition.CurrentUser, (DefaultWeeklyProjectPlanEntity ddp) => ddp.User.Is(UserEntity.Current));
        TypeConditionLogic.Register(RRSPCondition.CurrentUser, (PlanningGroupEntity gr) => gr.Users.Contains(UserEntity.Current));
        TypeConditionLogic.Register(RRSPCondition.CurrentUser, (VideoViewLogEntity vwl) => vwl.User.Is(UserEntity.Current));
        TypeConditionLogic.Register(RRSPCondition.CurrentUser, (WatchedProtocolPointEntity bh) => bh.User.Is(UserEntity.Current));
    }

    public static void ConfigureBigString(SchemaBuilder sb)
    {
        BigStringMode mode = BigStringMode.File;

        FileTypeLogic.Register(BigStringFileType.Exceptions, GetFileTypeAlgorithm(conf => conf.ExceptionsFolder));
        BigStringLogic.RegisterAll<ExceptionEntity>(sb, new BigStringConfiguration(mode, BigStringFileType.Exceptions));

        FileTypeLogic.Register(BigStringFileType.OperationLog, GetFileTypeAlgorithm(conf => conf.OperationLogFolder));
        BigStringLogic.RegisterAll<OperationLogEntity>(sb, new BigStringConfiguration(mode, BigStringFileType.OperationLog));

        FileTypeLogic.Register(BigStringFileType.EmailMessage, GetFileTypeAlgorithm(conf => conf.EmailMessageFolder));
        BigStringLogic.RegisterAll<EmailMessageEntity>(sb, new BigStringConfiguration(mode, BigStringFileType.EmailMessage));
    }//ConfigureBigString


    public static IFileTypeAlgorithm GetFileTypeAlgorithm(Func<FoldersConfigurationEmbedded, string> getFolder,
    bool weakFileReference = false,
    bool onlyImages = false,
    long? maxSizeInBytes = null,
    bool webDownload = false)
    {
        var folders = () => Starter.Configuration.Value.Folders;

        if (string.IsNullOrEmpty(AzureStorageConnectionString))
        {
            return new FileTypeAlgorithm(
                physicalPrefix: fp => Path.Combine(FilesRoot ?? "", getFolder(folders())),
                webPrefix: webDownload ? (fp => "~/" + getFolder(folders())) : null
                )
            {
                WeakFileReference = weakFileReference,
                OnlyImages = onlyImages,
                MaxSizeInBytes = maxSizeInBytes,
            };
        }
        else
            return new AzureBlobStorageFileTypeAlgorithm(fp => new Azure.Storage.Blobs.BlobContainerClient(
                AzureStorageConnectionString,
                getFolder(folders())))
            {
                CreateBlobContainerIfNotExists = true,
                WeakFileReference = weakFileReference,
                OnlyImages = onlyImages,
                SASTokenExpires = da => TimeSpan.FromDays(1),
                MaxSizeInBytes = maxSizeInBytes,
                WebDownload = () => webDownload ? AzureWebDownload.SASToken : AzureWebDownload.None,
                GetBlobAction = fp => webDownload ? BlobAction.Open : BlobAction.Download,
            };
    }

    public static void AddUserToProjects(UserEntity user, bool wasNew)
    {
        if (!PermissionAuthLogic.IsAuthorized(ProjectPermission.AddNewUserToProjects, user.Role))
            return;

        var oldRole = wasNew ? null : user.InDB(u => u.Role);

        if (oldRole != null && PermissionAuthLogic.IsAuthorized(ProjectPermission.AddNewUserToProjects, oldRole))
            return;

        if (user.IsNew)
            user.Save();

        using (OperationLogic.AllowSave<MemberEntity>())
        {
            if (Configuration.Value.DefaultProjectMemberRole != null)
                Database.Query<ProjectEntity>()
                    .Where(proj => proj.State == DomainState.Active && proj.Mixin<DomainTaskMixin>().AddNewUsersAutomatically && !proj.Members().Any(pm => pm.User.Is(user)))
                    .Select(p => new MemberEntity
                    {
                        Domain = p.ToLite(),
                        User = user.ToLite(),
                        Role = Configuration.Value.DefaultProjectMemberRole,
                        Deleted = false,
                    }).SaveList();
        }
    }

    //public class RRSPAuthorizer : AzureADAuthorizer
    //{
    //    public RRSPAuthorizer(Func<AzureADConfigurationEmbedded?> getConfig) : base(getConfig)
    //    {
    //    }

    //    public override UserEntity CreateUserInternal(IAutoCreateUserContext ctx)
    //    {
    //        var result = base.CreateUserInternal(ctx);

    //        var workDay = result.Role.ToString() == "Standard user" ? 8 /* "Normal" */: 0 /*"Exent"*/; 

    //        return result;
    //    }

    //    public override Lite<RoleEntity>? GetRole(IAutoCreateUserContext ctx, bool throwIfNull)
    //    {
    //        var config = this.GetConfig();
    //        if (ctx.OID != null && config != null)
    //        {
    //            var tokenCredential = AzureADLogic.GetTokenCredential();

    //            Microsoft.Graph.GraphServiceClient graphClient = new Microsoft.Graph.GraphServiceClient(tokenCredential);

    //            var user = graphClient.Users[ctx.OID!.Value.ToString()].GetAsync(a => {
    //                a.QueryParameters.Select = new[]
    //                {
    //                    nameof(Microsoft.Graph.Models.User.Id),
    //                    nameof(Microsoft.Graph.Models.User.DisplayName),
    //                    nameof(Microsoft.Graph.Models.User.OfficeLocation),
    //                    nameof(Microsoft.Graph.Models.User.CompanyName),
    //                    nameof(Microsoft.Graph.Models.User.OnPremisesExtensionAttributes),
    //                }.Select(a => a.FirstLower()).ToArray();

    //            }).Result;
                    
                    
    //            var role = config.RoleMapping.FirstOrDefault(m =>
    //            {
    //                var found = m.ADNameOrGuid == user!.Department;

    //                return found;
    //            });

    //            if (role != null)
    //                return role.Role;
    //        }

    //        return base.GetRole(ctx, throwIfNull);
    //    }

    //    public override void UpdateUserInternal(UserEntity user, IAutoCreateUserContext ctx)
    //    {
    //        base.UpdateUserInternal(user, ctx);

    //        user.Mixin<UserProjectMixin>().FirstName = ctx.FirstName;
    //        user.Mixin<UserProjectMixin>().LastName = ctx.LastName;
    //    }

    //    public override UserEntity OnCreateUser(IAutoCreateUserContext ctx)
    //    {
    //        var user = base.OnCreateUser(ctx);

    //        if (!PermissionAuthLogic.IsAuthorized(ProjectPermission.AddNewUserToProjects, user.Role))
    //            return user;

    //        using (OperationLogic.AllowSave<MemberEntity>())
    //        {
    //            if(Configuration.Value.DefaultProjectMemberRole != null)
    //            Database.Query<ProjectEntity>()
    //                .Where(a => a.State == ProjectState.Active && a.Mixin<DomainTaskMixin>().AddNewUsersAutomatically)
    //                .Select(p => new MemberEntity
    //                {
    //                    Domain = p.ToLite(),
    //                    User = user.ToLite(),
    //                    Role = Configuration.Value.DefaultProjectMemberRole,
    //                    Deleted = false,
    //                }).SaveList();
    //        }

    //        return user;
    //    }
    //}

    public class CustomSchemaBuilder : SchemaBuilder
    {
        public CustomSchemaBuilder() : base()
        {
        }

        public string? LogDatabaseName;

        public Type[] InLogDatabase = new Type[]
        {
            typeof(OperationLogEntity),
            typeof(ExceptionEntity),
        };

        public override DatabaseName? GetDatabase(Type type)
        {
            if (this.LogDatabaseName == null)
                return null;

            if (InLogDatabase.Contains(type))
                return new DatabaseName(null, this.LogDatabaseName, Settings.IsPostgres);

            return null;
        }

    }

    private static void OverrideAttributes(SchemaBuilder sb)
    {

        var allTypesInApplication = new[]{
            typeof(ApplicationConfigurationEntity),
            typeof(RevenueEntity),
            typeof(InitiationRequestEntity),
            typeof(ProjectEntity),
            typeof(MeetingProtocolEntity),
            typeof(RiskEntity),
            typeof(StakeholderEntity),
            typeof(StatementOfWorkEntity),
            typeof(StatusReportEntity),
            typeof(TaskEntity),
            typeof(VideoEntity),
            typeof(GoalEntity),
        }.SelectMany(t => t.Assembly.ExportedTypes.Where(t => t.IsEntity() && !t.IsAbstract)).ToList();

        //allTypesInApplication.Remove(typeof(OrganizationalUnitEntity));
        //allTypesInApplication.Remove(typeof(PortfolioEvaluationCriteriaEntity));
        //allTypesInApplication.Remove(typeof(PortfolioEvaluationDimensionEntity));
        //allTypesInApplication.Remove(typeof(PortfolioGlobalConfigurationEntity));
        //allTypesInApplication.Remove(typeof(CapacityRoleEntity));

        allTypesInApplication.ForEach(t =>
        {
            sb.Schema.Settings.TypeAttributes(t).Add(new SystemVersionedAttribute());
        });

        sb.Schema.Settings.FieldAttributes((ExceptionEntity ua) => ua.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((VisualTipConsumedEntity vt) => vt.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((ChangeLogViewLogEntity cl) => cl.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((OperationLogEntity ua) => ua.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((UserQueryEntity uq) => uq.Owner).Replace(new ImplementedByAttribute(typeof(UserEntity), typeof(RoleEntity)));
        sb.Schema.Settings.FieldAttributes((UserChartEntity uc) => uc.Owner).Replace(new ImplementedByAttribute(typeof(UserEntity), typeof(RoleEntity)));
        sb.Schema.Settings.FieldAttributes((DashboardEntity cp) => cp.Owner).Replace(new ImplementedByAttribute(typeof(UserEntity), typeof(RoleEntity)));

        sb.Schema.Settings.FieldAttributes((DashboardEntity a) => a.Parts.First().Content).Replace(new ImplementedByAttribute(
            typeof(UserChartPartEntity), typeof(CombinedUserChartPartEntity), typeof(UserQueryPartEntity), 
            typeof(BigValuePartEntity), typeof(ValueUserQueryListPartEntity), typeof(ToolbarMenuPartEntity), typeof(HealthCheckPartEntity), 
            typeof(TextPartEntity), typeof(CustomPartEntity)));

        sb.Schema.Settings.FieldAttributes((CachedQueryEntity a) => a.UserAssets.First()).Replace(new ImplementedByAttribute(typeof(UserQueryEntity), typeof(UserChartEntity)));

        sb.Schema.Settings.FieldAttributes((SystemEventLogEntity cp) => cp.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((AlertEntity a) => a.CreatedBy).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((AlertEntity a) => a.Recipient).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((AlertEntity a) => a.AttendedBy).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((PackageLineEntity cp) => cp.Package).Replace(new ImplementedByAttribute(typeof(PackageEntity), typeof(PackageOperationEntity)));
        sb.Schema.Settings.FieldAttributes((ProcessExceptionLineEntity cp) => cp.Line).Replace(new ImplementedByAttribute(typeof(PackageLineEntity)));
        sb.Schema.Settings.FieldAttributes((ProcessEntity cp) => cp.Data).Replace(new ImplementedByAttribute(typeof(PackageEntity), typeof(PackageOperationEntity), typeof(EmailPackageEntity)));
        sb.Schema.Settings.FieldAttributes((ProcessEntity s) => s.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((EmailMessageEntity em) => em.From.EmailOwner).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((EmailMessageEntity em) => em.Recipients.First().EmailOwner).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((EmailSenderConfigurationEntity em) => em.DefaultFrom!.EmailOwner).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((EmailSenderConfigurationEntity em) => em.AdditionalRecipients.First().EmailOwner).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((EmailSenderConfigurationEntity em) => em.Service).Replace(new ImplementedByAttribute(typeof(SmtpEmailServiceEntity), typeof(MicrosoftGraphEmailServiceEntity)));
        sb.Schema.Settings.FieldAttributes((ScheduledTaskEntity a) => a.Task).Replace(new ImplementedByAttribute(typeof(SimpleTaskSymbol), typeof(SendNotificationEmailTaskEntity)));
        sb.Schema.Settings.FieldAttributes((ScheduledTaskEntity a) => a.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));
        sb.Schema.Settings.FieldAttributes((ScheduledTaskLogEntity a) => a.Task).Replace(new ImplementedByAttribute(typeof(SimpleTaskSymbol), typeof(SendNotificationEmailTaskEntity)));
        sb.Schema.Settings.FieldAttributes((ScheduledTaskLogEntity a) => a.User).Replace(new ImplementedByAttribute(typeof(UserEntity)));

        //sb.Schema.Settings.FieldAttributes((ToolbarEntity tb) => tb.Elements.First().Content).Replace(new ImplementedByAttribute(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol)));
        sb.Schema.Settings.FieldAttributes((ToolbarMenuEntity tbm) => tbm.Elements.First().Content).Replace(new ImplementedByAttribute(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol)));
        sb.Schema.Settings.FieldAttributes((ToolbarEntity a) => a.Elements.First().Content).Replace(new ImplementedByAttribute(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol), typeof(ToolbarSwitcherEntity)));
        sb.Schema.Settings.FieldAttributes((TourStepEntity a) => a.CssSteps.First().ToolbarContent).Replace(new ImplementedByAttribute(typeof(ToolbarMenuEntity), typeof(ToolbarEntity), typeof(QueryEntity), typeof(UserQueryEntity), typeof(UserChartEntity), typeof(DashboardEntity), typeof(PermissionSymbol), typeof(ToolbarSwitcherEntity)));
        
        sb.Schema.Settings.FieldAttributes((NotificationConfigEntity nc) => nc.Target).Replace(new ImplementedByAttribute(typeof(ProjectEntity), typeof(ProgramEntity), typeof(TaskEntity), typeof(ColumnEntity)));
        
        sb.Schema.Settings.FieldAttributes((TaskEntity t) => t.CreatedFrom).Replace(new ImplementedByAttribute(typeof(ProtocolPointEntity), typeof(ProjectWorkPackageEntity)));

        Type[] PPP = new[] { typeof(ProjectEntity), typeof(ProgramEntity), typeof(PortfolioEntity) };

        sb.Schema.Settings.FieldAttributes((TaskLabelEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((TaskEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((BoardEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((PlanningGroupEntity a) => a.Domains.Single()).Replace(new ImplementedByAttribute(PPP));
        
        sb.Schema.Settings.FieldAttributes((NotificationConfigEntity nc) => nc.Target).Replace(new ImplementedByAttribute([.. PPP, typeof(TaskEntity), typeof(ColumnEntity)]));

        sb.Schema.Settings.FieldAttributes((DomainRoleEntity sr) => sr.OnlyFor).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((MemberEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((ExternalMemberEntity epm) => epm.Domain).Replace(new ImplementedByAttribute(PPP));

        sb.Schema.Settings.FieldAttributes((DecisionEntity d) => d.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((DecisionEntity d) => d.WaitingFor.First()).Replace(new ImplementedByAttribute(typeof(MemberEntity), typeof(ExternalMemberEntity), typeof(StakeholderEntity)));
        sb.Schema.Settings.FieldAttributes((DecisionEntity d) => d.DecidedBy.First()).Replace(new ImplementedByAttribute(typeof(MemberEntity), typeof(ExternalMemberEntity), typeof(StakeholderEntity)));

        sb.Schema.Settings.FieldAttributes((StakeholderEmailRecipientsModel s) => s.Stakeholders.First().Source).Replace(new ImplementedByAttribute(typeof(MemberEntity), typeof(ExternalMemberEntity), typeof(StakeholderEntity)));
        sb.Schema.Settings.FieldAttributes((StakeholderEmailRecipientsModel a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((MeetingProtocolEntity mp) => mp.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((ProtocolPointEntity pp) => pp.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((StakeholderEntity s) => s.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((StakeholderModel s) => s.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((StatusReportEntity sr) => sr.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((GoalEntity sr) => sr.Domain).Replace(new ImplementedByAttribute(PPP));

        sb.Schema.Settings.FieldAttributes((AssumptionEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((ChangeRequestEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((ConstraintEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((DeliverableEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((GoalEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((PSCImportModel a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((RequirementEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((ServiceEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((ServiceEstimationModel a) => a.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((RASCIMatrixEntity a) => a.Domain).Replace(new ImplementedByAttribute(PPP));

        sb.Schema.Settings.FieldAttributes((RiskEntity r) => r.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((RiskTemplateEntity r) => r.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((RiskEntity a) => a.EscalationHistory.First().From).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((RiskEntity a) => a.EscalationHistory.First().To).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((MilestoneEntity r) => r.Domain).Replace(new ImplementedByAttribute(PPP));
        
        sb.Schema.Settings.FieldAttributes((DomainReportingParentEntity r) => r.Child).Replace(new ImplementedByAttribute(typeof(ProjectEntity), typeof(ProgramEntity)));
        sb.Schema.Settings.FieldAttributes((DomainReportingParentEntity r) => r.Parent).Replace(new ImplementedByAttribute(typeof(ProgramEntity), typeof(PortfolioEntity)));
        sb.Schema.Settings.FieldAttributes((LessonsLearnedEntity l) => l.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((DomainAssignedLabelEntity l) => l.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((DomainAssignedLabelByParentEntity l) => l.Parent).Replace(new ImplementedByAttribute(typeof(ProgramEntity), typeof(PortfolioEntity)));
        sb.Schema.Settings.FieldAttributes((DomainAssignedLabelByParentEntity l) => l.Child).Replace(new ImplementedByAttribute(typeof(ProjectEntity), typeof(ProgramEntity)));

        sb.Schema.Settings.FieldAttributes((DynamicTaskScriptEntity p) => p.Domain).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((OrderProjectEntity p) => p.Domains.First()).Replace(new ImplementedByAttribute(PPP));
        sb.Schema.Settings.FieldAttributes((CustomerDomainEntity p) => p.Domain).Replace(new ImplementedByAttribute(PPP));

        sb.Schema.Settings.FieldAttributes((MeetingProtocolEntity mp) => mp.Customer).Replace(new ImplementedByAttribute());
        sb.Schema.Settings.FieldAttributes((ProtocolTemplateEntity mpt) => mpt.Customer).Replace(new ImplementedByAttribute());
        sb.Schema.Settings.FieldAttributes((StatusReportEntity sr) => sr.Customer).Replace(new ImplementedByAttribute());
        sb.Schema.Settings.FieldAttributes((StatusReportTemplateEntity sr) => sr.Customer).Replace(new ImplementedByAttribute());
        sb.Schema.Settings.FieldAttributes((StatementOfWorkEntity sow) => sow.Customer).Replace(new ImplementedByAttribute());
        sb.Schema.Settings.FieldAttributes((StatementOfWorkModel sow) => sow.Customer).Replace(new ImplementedByAttribute());
        sb.Schema.Settings.FieldAttributes((RiskEntity r) => r.Customer).Replace(new ImplementedByAttribute());
        sb.Schema.Settings.FieldAttributes((RiskTemplateEntity rt) => rt.Customer).Replace(new ImplementedByAttribute());

        sb.Schema.Settings.FieldAttributes((StatusReportEntity p) => p.ConsumedCost!.MaxCost).Replace(new FormatAttribute("K1"));
        sb.Schema.Settings.FieldAttributes((StatusReportEntity p) => p.ConsumedCost!.ConsumedCost).Replace(new FormatAttribute("K1"));
        sb.Schema.Settings.FieldAttributes((StatusReportEntity p) => p.ConsumedCost!.BilledCost).Replace(new FormatAttribute("K1"));
        sb.Schema.Settings.FieldAttributes((RiskEntity p) => p.Preventive!.ReducedRiskValue).Replace(new FormatAttribute("K1"));
        sb.Schema.Settings.FieldAttributes((RevenueEntity r) => r.Amount).Replace(new FormatAttribute("K1"));
        sb.Schema.Settings.FieldAttributes((ExpenditureEntity e) => e.Amount).Replace(new FormatAttribute("K1"));
    }
}
