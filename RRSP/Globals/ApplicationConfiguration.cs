using Signum.Mailing;
using Signum.Authorization;
using Signum.Word;
using Signum.Files;
using Signum.Authorization.AuthToken;
using Signum.Authorization.Rules;
using Meros.Project;
using Meros.Tasks;
using Signum.Tour;
using Signum.Dashboard;

namespace RRSP.Globals;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class ApplicationConfigurationEntity : Entity
{
    [StringLengthValidator(Min = 3, Max = 100), XmlStringValidator]
    public string Environment { get; set; }

    [StringLengthValidator(Min = 3, Max = 100), XmlStringValidator]
    public string DatabaseName { get; set; }

    /*Email*/
    public EmailConfigurationEmbedded Email { get; set; }

    public Lite<UserEntity> DefaultAccountingReceiver { get; set; }

    public Lite<WordTemplateEntity>? RiskManagementReportTemplate { get; set; }

    public Lite<WordTemplateEntity>? RiskManagementPercentReportTemplate { get; set; }

    public Lite<WordTemplateEntity>? ContactPersonReportTemplate { get; set; }
    
    public Lite<WordTemplateEntity>? MeetingProtocolReportTemplate { get; set; }
    
    public Lite<WordTemplateEntity>? ChangeRequestReportTemplate { get; set; }
    
    public Lite<WordTemplateEntity>? WorkPackageReportTemplate { get; set; }
    
    public Lite<WordTemplateEntity>? StatusReportReportTemplate { get; set; }
    
    public Lite<WordTemplateEntity>? BusinessCaseReportTemplate { get; set; }

    public Lite<WordTemplateEntity>? ProjectCharterReportTemplate { get; set; }
        
    public Lite<DomainRoleEntity>? DefaultProjectManagerRole { get; set; }
    public Lite<DomainRoleEntity>? DefaultProjectMemberRole { get; set; }
    public Lite<DomainRoleEntity>? DefaultProgramMangerRole { get; set; }
    public Lite<DomainRoleEntity>? DefaultPortfolioMangerRole { get; set; }

    public EmailSenderConfigurationEntity EmailSender { get; set; }
    /*AuthTokens*/
    public AuthTokenConfigurationEmbedded AuthTokens { get; set; }

    public FoldersConfigurationEmbedded Folders { get; set; }

    [StringLengthValidator(MultiLine = true), XmlStringValidator]
    public string? ExtraValidAudiences { get; set; }

    public TranslationConfigurationEmbedded Translation { get; set; }

    public TaskConfigEmbedded Task { get; set; }

    public FileEmbedded? MeetingProtocolExcelTemplate { get; set; }

    public DefaultDashboardEmbedded? DefaultDashboard { get; set; }

    public decimal? PerDayPrice { get; set; }

    [StringLengthValidator(MultiLine =true), XmlStringValidator]
    public string? EmailSignature { get; set; }
}

public class DefaultDashboardEmbedded : EmbeddedEntity
{
    public Lite<DashboardEntity> Portfolio { get; set; }
    public Lite<DashboardEntity> Program { get; set; }
    public Lite<DashboardEntity> Project { get; set; }
}

[AutoInit]
public static class ApplicationConfigurationOperation
{
    public static ExecuteSymbol<ApplicationConfigurationEntity> Save;
}

[AutoInit]
public static class UserExpandedOperation
{
    public static DeleteSymbol<UserEntity> DeleteWithAlternative;
}

[AutoInit]
public static class WordTemplateExpandedOperation
{
    public static ConstructSymbol<WordTemplateEntity>.From<WordTemplateEntity> Clone;
}

[AutoInit]
public static class RRSPTourTriggers
{
    public static readonly TourTriggerSymbol Introduction;
}

[AutoInit]
public static class RRSPWordConverter
{
    public static readonly WordConverterSymbol AsposeToPdf;
    public static readonly WordConverterSymbol AsposeToPdfWithAttachments;
}

[AutoInit]
public static class RRSPWordTransformer
{
    public static readonly WordTransformerSymbol UpdateProjectStatusReport;
    public static readonly WordTransformerSymbol InsertRiskCategoryTable;
}

[AutoInit]
public static class RRSPCondition
{
    public static TypeConditionSymbol UserEntities;
    public static TypeConditionSymbol RoleEntities;

    public static TypeConditionSymbol CurrentUser;
    public static TypeConditionSymbol SupervisedUsers;
    public static TypeConditionSymbol Billed;

    public static TypeConditionSymbol CurrentUserLastTwoWeeks;
    public static TypeConditionSymbol IsoReportCreator;

    public static TypeConditionSymbol Published;

    public static TypeConditionSymbol AllowedRole;
    
    public static TypeConditionSymbol IsoAuditor;
    
    public static TypeConditionSymbol PersonInCharge;
    
    public static TypeConditionSymbol SOWEditor;
    
    public static TypeConditionSymbol TimeApprover;
}

public class FoldersConfigurationEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string BillingDocumentFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string AttachmentsFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string ExceptionsFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string OperationLogFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string EmailMessageFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string CachedQueryFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string ViewLogFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string VideosFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string VideoThumbnailsFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string VideoInlineImagesFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string ProjectStatusReportFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string ProtocolReportFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string DelimitationDocumentFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator, XmlStringValidator]
    public string CommunicationLetterFolder { get; set; }

    [StringLengthValidator(Max = 300), AzureStorageCollectionNameValidation]
    public string WhatsNewDocumentFolder { get; set; }

    [StringLengthValidator(Max = 300), AzureStorageCollectionNameValidation]
    public string WhatsNewPreviewFolder { get; set; }

    [StringLengthValidator(Max = 300), AzureStorageCollectionNameValidation]
    public string ExternalBillableAttachmentFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string SkillCertificateFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string HelpImagesFolder{ get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string StatementOfWorkReportFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string InvestmentReportFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string InvestmentAttachmentFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string ContractAttachmentsFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string ChangeRequestReportFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string ChangeRequestAttachmentFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string WorkPackageReportFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string WorkPackageAttachmentFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string ProjectChartFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string DocumentQMAttachmentFolder { get; set; }

    [StringLengthValidator(Max = 300), FileNameValidator]
    public string BusinessCaseBaseLineFolder { get; set; }
}

public class TranslationConfigurationEmbedded : EmbeddedEntity
{
    [Description("Azure Cognitive Service API Key")]
    [StringLengthValidator(Max = 300), FileNameValidator]
    public string? AzureCognitiveServicesAPIKey { get; set; }

    [Description("Azure Cognitive Service Region")]
    [StringLengthValidator(Max = 300), FileNameValidator]
    public string? AzureCognitiveServicesRegion { get; set; }

    [Description("DeepL API Key")]
    [StringLengthValidator(Max = 300), FileNameValidator]
    public string? DeepLAPIKey { get; set; }
}

[AutoInit]
public static class BigStringFileType
{
    public static readonly FileTypeSymbol Exceptions;
    public static readonly FileTypeSymbol OperationLog;
    public static readonly FileTypeSymbol ViewLog;
    public static readonly FileTypeSymbol EmailMessage;
}

[AutoInit]
public static class LoginPermission
{
    public static PermissionSymbol ChangePassword;
}

public class RoleMixin : MixinEntity
{
    RoleMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }

    public bool AdministratedByBillingUser { get; set; }
    public RoleType Type { get; set; }
}

[AllowUnauthenticated]
public enum RoleType
{
    Internal,
    External
}

[AllowUnauthenticated]
public enum RRSPMessage
{
    [Description("Welcome to Agile 360")]
    WelcomeToRRSP,
    [Description("A 360° solution to manage the development of software")]
    A360SolutionToManageTheDevelopmentOfSoftware,
}

public enum GlobalMessage
{
    [Description("Please select an alternative {0}")]
    PleaseSelectAnAlternative0,
    [Description("Projects / Programs / Portfolios")]
    ProjectsProgramsPortfolios,
    CommunicationManagement,
    [Description("Tasks appear here as soon as they are linked to a stakeholder.")]
    TasksAppearHereAsSoonAsTheyAreLinkedToAStakeholder,
    ActiveProjects,
    DefaultPortfolioManagerRoleIsNotConfigured,
    DefaultProgramManagerRoleIsNotConfigured,
    PasswordComplexityWarning,
    [Description("Password must contain at least one uppercase letter")]
    PasswordMustContainUppercase,
    [Description("Password must contain at least one lowercase letter")]
    PasswordMustContainLowercase,
    [Description("Password must contain at least one digit")]
    PasswordMustContainDigit,
    [Description("Password must contain at least one special character")]
    PasswordMustContainSpecialChar
}

[EntityKind(EntityKind.String, EntityData.Master)]
public class RessortEntity : Entity
{
    [StringLengthValidator(Max = 100)]
    public string Name { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

[AutoInit]
public static class RessortOperation
{
    public static readonly ExecuteSymbol<RessortEntity> Save;
    public static readonly DeleteSymbol<RessortEntity> Delete;
}

[EntityKind(EntityKind.String, EntityData.Master)]
public class ReferatEntity : Entity
{
    [StringLengthValidator(Max = 100)]
    public string Name { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);
}

[AutoInit]
public static class ReferatOperation
{
    public static readonly ExecuteSymbol<ReferatEntity> Save;
    public static readonly DeleteSymbol<ReferatEntity> Delete;
}

public class UserMixin : MixinEntity
{
    UserMixin(ModifiableEntity mainEntity, MixinEntity? next)
        : base(mainEntity, next)
    {
    }
    public Lite<RessortEntity>? Ressort { get; set; }
    public Lite<ReferatEntity>? Referat { get; set; }
}
