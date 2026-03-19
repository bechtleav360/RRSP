//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Framework/Signum/React/Reflection'
import * as Entities from '../../Framework/Signum/React/Signum.Entities'
import * as Operations from '../../Framework/Signum/React/Signum.Operations'
import * as Basics from '../../Framework/Signum/React/Signum.Basics'
import * as Mailing from '../../Framework/Extensions/Signum.Mailing/Signum.Mailing'
import * as Authorization from '../../Framework/Extensions/Signum.Authorization/Signum.Authorization'
import * as AuthToken from '../../Framework/Extensions/Signum.Authorization/AuthToken/Signum.Authorization.AuthToken'
import * as Rules from '../../Framework/Extensions/Signum.Authorization/Rules/Signum.Authorization.Rules'
import * as Word from '../../Framework/Extensions/Signum.Word/Signum.Word'
import * as Project from '../../Meros/Meros.Project/Meros.Project'
import * as Tasks from '../../Meros/Meros.Tasks/Meros.Tasks'
import * as Files from '../../Framework/Extensions/Signum.Files/Signum.Files'
import * as Dashboard from '../../Framework/Extensions/Signum.Dashboard/Signum.Dashboard'
import * as Tour from '../../Framework/Extensions/Signum.Tour/Signum.Tour'


export const ApplicationConfigurationEntity: Type<ApplicationConfigurationEntity> = new Type<ApplicationConfigurationEntity>("ApplicationConfiguration");
export interface ApplicationConfigurationEntity extends Entities.Entity {
  Type: "ApplicationConfiguration";
  environment: string;
  databaseName: string;
  email: Mailing.EmailConfigurationEmbedded;
  defaultAccountingReceiver: Entities.Lite<Authorization.UserEntity>;
  riskManagementReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  riskManagementPercentReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  contactPersonReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  meetingProtocolReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  changeRequestReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  workPackageReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  statusReportReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  businessCaseReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  projectCharterReportTemplate: Entities.Lite<Word.WordTemplateEntity> | null;
  defaultProjectManagerRole: Entities.Lite<Project.DomainRoleEntity> | null;
  defaultProjectMemberRole: Entities.Lite<Project.DomainRoleEntity> | null;
  defaultProgramMangerRole: Entities.Lite<Project.DomainRoleEntity> | null;
  defaultPortfolioMangerRole: Entities.Lite<Project.DomainRoleEntity> | null;
  emailSender: Mailing.EmailSenderConfigurationEntity;
  authTokens: AuthToken.AuthTokenConfigurationEmbedded;
  folders: FoldersConfigurationEmbedded;
  extraValidAudiences: string | null;
  translation: TranslationConfigurationEmbedded;
  task: Tasks.TaskConfigEmbedded;
  meetingProtocolExcelTemplate: Files.FileEmbedded | null;
  defaultDashboard: DefaultDashboardEmbedded | null;
  perDayPrice: number | null;
  emailSignature: string | null;
}

export namespace ApplicationConfigurationOperation {
  export const Save : Operations.ExecuteSymbol<ApplicationConfigurationEntity> = registerSymbol("Operation", "ApplicationConfigurationOperation.Save");
}

export namespace BigStringFileType {
  export const Exceptions : Files.FileTypeSymbol = registerSymbol("FileType", "BigStringFileType.Exceptions");
  export const OperationLog : Files.FileTypeSymbol = registerSymbol("FileType", "BigStringFileType.OperationLog");
  export const ViewLog : Files.FileTypeSymbol = registerSymbol("FileType", "BigStringFileType.ViewLog");
  export const EmailMessage : Files.FileTypeSymbol = registerSymbol("FileType", "BigStringFileType.EmailMessage");
}

export const DefaultDashboardEmbedded: Type<DefaultDashboardEmbedded> = new Type<DefaultDashboardEmbedded>("DefaultDashboardEmbedded");
export interface DefaultDashboardEmbedded extends Entities.EmbeddedEntity {
  Type: "DefaultDashboardEmbedded";
  portfolio: Entities.Lite<Dashboard.DashboardEntity>;
  program: Entities.Lite<Dashboard.DashboardEntity>;
  project: Entities.Lite<Dashboard.DashboardEntity>;
}

export const FoldersConfigurationEmbedded: Type<FoldersConfigurationEmbedded> = new Type<FoldersConfigurationEmbedded>("FoldersConfigurationEmbedded");
export interface FoldersConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "FoldersConfigurationEmbedded";
  billingDocumentFolder: string;
  attachmentsFolder: string;
  exceptionsFolder: string;
  operationLogFolder: string;
  emailMessageFolder: string;
  cachedQueryFolder: string;
  viewLogFolder: string;
  videosFolder: string;
  videoThumbnailsFolder: string;
  videoInlineImagesFolder: string;
  projectStatusReportFolder: string;
  protocolReportFolder: string;
  delimitationDocumentFolder: string;
  communicationLetterFolder: string;
  whatsNewDocumentFolder: string;
  whatsNewPreviewFolder: string;
  externalBillableAttachmentFolder: string;
  skillCertificateFolder: string;
  helpImagesFolder: string;
  statementOfWorkReportFolder: string;
  investmentReportFolder: string;
  investmentAttachmentFolder: string;
  contractAttachmentsFolder: string;
  changeRequestReportFolder: string;
  changeRequestAttachmentFolder: string;
  workPackageReportFolder: string;
  workPackageAttachmentFolder: string;
  projectChartFolder: string;
  documentQMAttachmentFolder: string;
  businessCaseBaseLineFolder: string;
}

export namespace GlobalMessage {
  export const PleaseSelectAnAlternative0: MessageKey = new MessageKey("GlobalMessage", "PleaseSelectAnAlternative0");
  export const ProjectsProgramsPortfolios: MessageKey = new MessageKey("GlobalMessage", "ProjectsProgramsPortfolios");
  export const CommunicationManagement: MessageKey = new MessageKey("GlobalMessage", "CommunicationManagement");
  export const TasksAppearHereAsSoonAsTheyAreLinkedToAStakeholder: MessageKey = new MessageKey("GlobalMessage", "TasksAppearHereAsSoonAsTheyAreLinkedToAStakeholder");
  export const ActiveProjects: MessageKey = new MessageKey("GlobalMessage", "ActiveProjects");
  export const DefaultPortfolioManagerRoleIsNotConfigured: MessageKey = new MessageKey("GlobalMessage", "DefaultPortfolioManagerRoleIsNotConfigured");
  export const DefaultProgramManagerRoleIsNotConfigured: MessageKey = new MessageKey("GlobalMessage", "DefaultProgramManagerRoleIsNotConfigured");
  export const PasswordComplexityWarning: MessageKey = new MessageKey("GlobalMessage", "PasswordComplexityWarning");
  export const PasswordMustContainUppercase: MessageKey = new MessageKey("GlobalMessage", "PasswordMustContainUppercase");
  export const PasswordMustContainLowercase: MessageKey = new MessageKey("GlobalMessage", "PasswordMustContainLowercase");
  export const PasswordMustContainDigit: MessageKey = new MessageKey("GlobalMessage", "PasswordMustContainDigit");
  export const PasswordMustContainSpecialChar: MessageKey = new MessageKey("GlobalMessage", "PasswordMustContainSpecialChar");
}

export namespace LoginPermission {
  export const ChangePassword : Basics.PermissionSymbol = registerSymbol("Permission", "LoginPermission.ChangePassword");
}

export const ReferatEntity: Type<ReferatEntity> = new Type<ReferatEntity>("Referat");
export interface ReferatEntity extends Entities.Entity {
  Type: "Referat";
  name: string;
}

export namespace ReferatOperation {
  export const Save : Operations.ExecuteSymbol<ReferatEntity> = registerSymbol("Operation", "ReferatOperation.Save");
  export const Delete : Operations.DeleteSymbol<ReferatEntity> = registerSymbol("Operation", "ReferatOperation.Delete");
}

export const RessortEntity: Type<RessortEntity> = new Type<RessortEntity>("Ressort");
export interface RessortEntity extends Entities.Entity {
  Type: "Ressort";
  name: string;
}

export namespace RessortOperation {
  export const Save : Operations.ExecuteSymbol<RessortEntity> = registerSymbol("Operation", "RessortOperation.Save");
  export const Delete : Operations.DeleteSymbol<RessortEntity> = registerSymbol("Operation", "RessortOperation.Delete");
}

export const RoleMixin: Type<RoleMixin> = new Type<RoleMixin>("RoleMixin");
export interface RoleMixin extends Entities.MixinEntity {
  Type: "RoleMixin";
  administratedByBillingUser: boolean;
  type: RoleType;
}

export const RoleType: EnumType<RoleType> = new EnumType<RoleType>("RoleType");
export type RoleType =
  "Internal" |
  "External";

export namespace RRSPCondition {
  export const UserEntities : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.UserEntities");
  export const RoleEntities : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.RoleEntities");
  export const CurrentUser : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.CurrentUser");
  export const SupervisedUsers : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.SupervisedUsers");
  export const Billed : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.Billed");
  export const CurrentUserLastTwoWeeks : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.CurrentUserLastTwoWeeks");
  export const IsoReportCreator : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.IsoReportCreator");
  export const Published : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.Published");
  export const AllowedRole : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.AllowedRole");
  export const IsoAuditor : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.IsoAuditor");
  export const PersonInCharge : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.PersonInCharge");
  export const SOWEditor : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.SOWEditor");
  export const TimeApprover : Rules.TypeConditionSymbol = registerSymbol("TypeCondition", "RRSPCondition.TimeApprover");
}

export namespace RRSPMessage {
  export const WelcomeToRRSP: MessageKey = new MessageKey("RRSPMessage", "WelcomeToRRSP");
  export const A360SolutionToManageTheDevelopmentOfSoftware: MessageKey = new MessageKey("RRSPMessage", "A360SolutionToManageTheDevelopmentOfSoftware");
}

export namespace RRSPTourTriggers {
  export const Introduction : Tour.TourTriggerSymbol = registerSymbol("TourTrigger", "RRSPTourTriggers.Introduction");
}

export namespace RRSPWordConverter {
  export const AsposeToPdf : Word.WordConverterSymbol = registerSymbol("WordConverter", "RRSPWordConverter.AsposeToPdf");
  export const AsposeToPdfWithAttachments : Word.WordConverterSymbol = registerSymbol("WordConverter", "RRSPWordConverter.AsposeToPdfWithAttachments");
}

export namespace RRSPWordTransformer {
  export const UpdateProjectStatusReport : Word.WordTransformerSymbol = registerSymbol("WordTransformer", "RRSPWordTransformer.UpdateProjectStatusReport");
  export const InsertRiskCategoryTable : Word.WordTransformerSymbol = registerSymbol("WordTransformer", "RRSPWordTransformer.InsertRiskCategoryTable");
}

export const TranslationConfigurationEmbedded: Type<TranslationConfigurationEmbedded> = new Type<TranslationConfigurationEmbedded>("TranslationConfigurationEmbedded");
export interface TranslationConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "TranslationConfigurationEmbedded";
  azureCognitiveServicesAPIKey: string | null;
  azureCognitiveServicesRegion: string | null;
  deepLAPIKey: string | null;
}

export namespace UserExpandedOperation {
  export const DeleteWithAlternative : Operations.DeleteSymbol<Authorization.UserEntity> = registerSymbol("Operation", "UserExpandedOperation.DeleteWithAlternative");
}

export const UserMixin: Type<UserMixin> = new Type<UserMixin>("UserMixin");
export interface UserMixin extends Entities.MixinEntity {
  Type: "UserMixin";
  ressort: Entities.Lite<RessortEntity> | null;
  referat: Entities.Lite<ReferatEntity> | null;
}

export namespace WordTemplateExpandedOperation {
  export const Clone : Operations.ConstructSymbol_From<Word.WordTemplateEntity, Word.WordTemplateEntity> = registerSymbol("Operation", "WordTemplateExpandedOperation.Clone");
}

