using Meros.PlanningProject.TaskManagement;
using Meros.Project;
using Meros.Protocol;
using Meros.Tasks;
using RRSP.Globals;
using Signum.Authorization;
using Signum.Authorization.AuthToken;
using Signum.Basics;
using Signum.Mailing;
using Signum.Security;
using Signum.UserAssets;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace RRSP.Test.Environment;

class BasicLoader
{
    internal static void LoadUsers()
    {
        var roles = Database.Query<RoleEntity>().ToDictionary(a => a.Name);

        CreateUser("System", "System", "Robot", roles.GetOrThrow("Super user"));
        CreateUser("sara.super", "Sara", "Super", roles.GetOrThrow("Super user"));
        //CreateUser("billy.billing", "Billy", "Billing", roles.GetOrThrow("Billing user"));
        CreateUser("maggie.manager", "Maggie", "Manager", roles.GetOrThrow("Advanced user"));
        CreateUser("john.java", "John", "Java", roles.GetOrThrow("Standard user"));
        CreateUser("thomas.typescript", "Thomas", "Typescript", roles.GetOrThrow("Standard user"));
        CreateUser("dennis.dotnet", "Dennis", "Dotnet", roles.GetOrThrow("Standard user"));
        CreateUser("robin.react", "Robin", "React", roles.GetOrThrow("Standard user"));
        CreateUser("thomas.muenzer", "Thomas", "Münzer", roles.GetOrThrow("Super user"));

        for (int i = 0; i < 50; i++)
        {
            CreateUser("TestPerson" + i.ToString(), "Test", "Person" + i.ToString(), roles.GetOrThrow("Standard user"));
        }
    }

    private static UserEntity CreateUser(string username, string firstName, string lastName, RoleEntity role)
    {
        UserEntity user = new UserEntity
        {
            UserName = username,
            Email =  username == "System" ? null : username + "@bechtle.com",
            PasswordHash = PasswordEncoding.EncodePassword(username, username),
            Role = role.ToLite(),
            State = UserState.Active,
        };

        user.Mixin<UserProjectMixin>().FirstName = firstName;
        user.Mixin<UserProjectMixin>().LastName = lastName;
        user.Save();

        return user; 
    }

    internal static void LoadBasics()
    {
        var en = new CultureInfoEntity(CultureInfo.GetCultureInfo("en")).Save();
        var de = new CultureInfoEntity(CultureInfo.GetCultureInfo("de")).Save();
        new CultureInfoEntity(CultureInfo.GetCultureInfo("en-GB")).Save();
        new CultureInfoEntity(CultureInfo.GetCultureInfo("de-DE")).Save();

        new ApplicationConfigurationEntity
        {
            Environment = "Test",
            DatabaseName = Connector.Current.OriginalDatabaseName(),
            Email = new EmailConfigurationEmbedded
            {
                SendEmails = false,
                DefaultCulture = de,
                UrlLeft = "http://localhost/RRSP",
            },
            AuthTokens = new AuthTokenConfigurationEmbedded
            {
            }, //Auth
            EmailSender = new EmailSenderConfigurationEntity
            {
                Name = "localhost",
                Service = new SmtpEmailServiceEntity
                {
                    Network = new SmtpNetworkDeliveryEmbedded
                    {
                        Host = "localhost"
                    }
                }
            }, //Email
            Folders = new FoldersConfigurationEmbedded
            {
                AttachmentsFolder = @"c:/RRSP/Attachments",
                BillingDocumentFolder = @"c:/RRSP/BillingDocumentFolder",
                ExceptionsFolder = @"c:/RRSP/exception",
                OperationLogFolder = @"c:/RRSP/operation-log",
                EmailMessageFolder = @"c:/RRSP/email-message",
                ViewLogFolder = @"c:/RRSP/view-log",
                CachedQueryFolder = @"c:/RRSP/cached-query",
                VideosFolder = @"c:/RRSP/videos",
                VideoThumbnailsFolder = @"c:/RRSP/video-thumbnails",
                ProjectStatusReportFolder = @"c:/RRSP/project-status-report",
                ProtocolReportFolder = @"c:/RRSP/protocol-report",
                DelimitationDocumentFolder = @"c:/RRSP/delimitation-document",
                CommunicationLetterFolder = @"c:/RRSP/communication-letter",
                ExternalBillableAttachmentFolder= @"c:/RRSP/external-billable-attachment",
                SkillCertificateFolder= @"c:/RRSP/skill-certificate",
                WhatsNewDocumentFolder = @"c:/RRSP/whatsnew-document",
                WhatsNewPreviewFolder = @"c:/RRSP/whatsnew-preview",
                VideoInlineImagesFolder = @"c:/RRSP/video-image",
                HelpImagesFolder = @"c:/RRSP/help-image",
                StatementOfWorkReportFolder = @"c:/RRSP/statement-of-work",
                ContractAttachmentsFolder = @"contract-attachment",
                InvestmentReportFolder = @"c:/RRSP/investment-report",
                InvestmentAttachmentFolder = @"c:/RRSP/investment-attachment",
                ChangeRequestReportFolder = @"c:/RRSP/change-request-report",
                ChangeRequestAttachmentFolder = @"c:/RRSP/change-request-attachment",
                WorkPackageReportFolder = @"c:/RRSP/work-package-report",
                WorkPackageAttachmentFolder = @"c:/RRSP/work-package-attachment",
                ProjectChartFolder = @"c:/RRSP/project-chart",
                DocumentQMAttachmentFolder = @"c:/RRSP/document-qm-aatachment",
                BusinessCaseBaseLineFolder = @"c:/RRSP/buisness-case-baseline",
            },
            //ActiveDirectory = new ActiveDirectoryConfigurationEmbedded
            //{
            //    WindowsAD = new WindowsActiveDirectoryEmbedded
            //    {
            //        DomainName = "BECHTLE",
            //    },
            //    AzureAD = null,
            //},
            Translation = new TranslationConfigurationEmbedded
            {
            },
            Task = new TaskConfigEmbedded
            {

            },
            DefaultAccountingReceiver = Database.Query<UserEntity>().Single(a => a.UserName == "System").ToLite(),
        }.Save();

        //using (UserHolder.UserSession(AuthLogic.SystemUser!)) No user to avoid simplification before AuthRules!!
        UserAssetsImporter.ImportAll(File.ReadAllBytes(@"..\..\..\..\RRSP.Terminal\DomainRoles.xml"));

        var appConfig = Database.Query<ApplicationConfigurationEntity>().Single();
        appConfig.DefaultProjectManagerRole = Database.Query<DomainRoleEntity>().Single(a => a.Name == "PM").ToLite();
        appConfig.DefaultProjectMemberRole = Database.Query<DomainRoleEntity>().Single(a => a.Name == "Projektkernteam").ToLite();
        appConfig.Save();
    }

    public static void CreateProtocolMasterData()
    {
        new ProtocolPointTypeEntity { Abbreviation = "A", Name = "Aufgabe" }.SetMixin((ProtocolPointTypeTaskMixin pptm) => pptm.CreateTask, true).Save();
        new ProtocolPointTypeEntity { Abbreviation = "D", Name = "Dokumentation" }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "E", Name = "Entscheidung" }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "I", Name = "Information", IsDefault = true }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "F", Name = "Frage" }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "T", Name = "Termin" }.Save();
        new ProtocolPointTypeEntity { Abbreviation = "P", Name = "Problem" }.Save();

        new MeetingTypeEntity { Name = "Auftraggeber - Jourfixe" }.Save();
        new MeetingTypeEntity { Name = "Projektbesprechung" }.Save();
        new MeetingTypeEntity { Name = "Lenkungsausschuss" }.Save();
        new MeetingTypeEntity { Name = "Technische Besprechung" }.Save();
        new MeetingTypeEntity { Name = "Sonstige Besprechung" }.Save();
        new MeetingTypeEntity { Name = "Telefongespräch", ForProtocolPoint = true }.Save();
        new MeetingTypeEntity { Name = "Team - Chat", ForProtocolPoint = true }.Save();
        new MeetingTypeEntity { Name = "E - Mail", ForProtocolPoint = true }.Save();

        new ProtocolPointDecisionEntity { Name = "Keine" }.Save();
        new ProtocolPointDecisionEntity { Name = "Entschieden" }.Save();
        new ProtocolPointDecisionEntity { Name = "Ausstehend Kunde" }.Save();
        new ProtocolPointDecisionEntity { Name = "Ausstehend Intern" }.Save();
    }

}
