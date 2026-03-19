import { RouteObject } from "react-router"
import { Navigator } from "@framework/Navigator"
import { Operations } from "@framework/Operations"
import { Finder } from "@framework/Finder"
import { QuickLinkClient } from "@framework/QuickLinkClient"
import { VisualTipClient } from "@framework/Basics/VisualTipClient"
import { ChangeLogClient } from "@framework/Basics/ChangeLogClient"
import { ExceptionClient } from "@framework/Exceptions/ExceptionClient"

import { AuthAdminClient } from "@extensions/Signum.Authorization/AuthAdminClient"
import { UserQueryClient } from "@extensions/Signum.UserQueries/UserQueryClient"
import { OmniboxClient } from "@extensions/Signum.Omnibox/OmniboxClient"
import { ChartClient } from "@extensions/Signum.Chart/ChartClient"
import { WhatsNewClient } from "@extensions/Signum.WhatsNew/WhatsNewClient"
import { DashboardClient } from "@extensions/Signum.Dashboard/DashboardClient"
import { MapClient } from "@extensions/Signum.Map/MapClient"
import { CacheClient } from "@extensions/Signum.Caching/CacheClient"
import { ProcessClient } from "@extensions/Signum.Processes/ProcessClient"
import { MailingClient } from "@extensions/Signum.Mailing/MailingClient"
import { ProfilerClient } from "@extensions/Signum.Profiler/ProfilerClient"
import { TimeMachineClient } from "@extensions/Signum.TimeMachine/TimeMachineClient"
import { FilesClient } from "@extensions/Signum.Files/FilesClient"
import { WordClient } from "@extensions/Signum.Word/WordClient"
import { ExcelClient } from "@extensions/Signum.Excel/ExcelClient"
import { SchedulerClient } from "@extensions/Signum.Scheduler/SchedulerClient"
import { TranslationClient } from "@extensions/Signum.Translation/TranslationClient"
import { TranslatedInstanceClient } from "@extensions/Signum.Translation/TranslatedInstanceClient"
import { DiffLogClient } from "@extensions/Signum.DiffLog/DiffLogClient"
import { ToolbarClient } from "@extensions/Signum.Toolbar/ToolbarClient"
import { SubsClient } from "@extensions/Signum.Toolbar/Subs/SubsClient"
import { AlertsClient } from "@extensions/Signum.Alerts/AlertsClient"
import { ConcurrentUserClient } from "@extensions/Signum.ConcurrentUser/ConcurrentUserClient"
import { GlobalsClient } from "./Globals/GlobalsClient"
import { HelpClient } from "@extensions/Signum.Help/HelpClient"
import { PropertyRoute } from "@framework/Reflection"
import { TaskEntity } from "../Meros/Meros.Tasks/Meros.Tasks"
import { ProjectEntity } from "../Meros/Meros.Project/Meros.Project"
import { ProjectClient } from "../Meros/Meros.Project/ProjectClient"
import { PortfolioClient } from "../Meros/Meros.Project/Portfolio/PortfolioClient"
import { ProgramClient } from "../Meros/Meros.Project/Program/ProgramClient"
import { TasksClient } from "../Meros/Meros.Tasks/TasksClient"
import { VideoClient } from "../Meros/Meros.Videos/VideoClient"
import { ProtocolClient } from "../Meros/Meros.Protocol/ProtocolClient"
import { DecisionClient } from "../Meros/Meros.Protocol/Decision/DecisionClient"
import { BusinessCaseClient } from "../Meros/Meros.PlanningProject/BusinessCase/BusinessCaseClient"
import { BaseClient } from "../Meros/Meros.PlanningProject/Base/BaseClient"
import { MyProjectsClient } from "../Meros/Meros.PortfolioExt/MyProjects/MyProjectsClient"
import { MyProgramsClient } from "../Meros/Meros.PortfolioExt/MyPrograms/MyProgramsClient"
import { MyPortfoliosClient } from "../Meros/Meros.PortfolioExt/MyPortfolios/MyPortfoliosClient"
import { PSCClient } from "../Meros/Meros.PlanningProject/PSC/PSCClient"
import { RASCIClient } from "../Meros/Meros.PlanningProject/RASCI/RASCIClient"
import { WorkPackageClient } from "../Meros/Meros.PlanningProject/WorkPackage/WorkPackageClient"
import { TaskManagementClient } from "../Meros/Meros.PlanningProject/TaskManagement/TaskManagementClient"
import { StatusReportClient } from "../Meros/Meros.StatusReport/StatusReportClient"
import { RiskClient } from "../Meros/Meros.Risk/RiskClient"
import { FinancialsClient } from "../Meros/Meros.Financials/FinancialsClient"
import { StakeholderClient } from "../Meros/Meros.Stakeholder/StakeholderClient"
import { StatementOfWorkClient } from "../Meros/Meros.StatementOfWork/StatementOfWorkClient"
import { PortfolioExtClient } from "../Meros/Meros.PortfolioExt/PortfolioExtClient"
import { InitiationRequestClient } from "../Meros/Meros.PortfolioExt/InitiationRequest/InitiationRequestClient"
import { ProjectPrioritizationClient } from "../Meros/Meros.PortfolioExt/InitiationRequest/ProjectPrioritizationClient"
import { PlanOverviewClient } from "../Meros/Meros.PortfolioExt/PlanOverview/PlanOverviewClient"
import { PortfolioRelationshipClient } from "../Meros/Meros.PortfolioExt/PortfolioRelationship/PortfolioRelationshipClient"
import { ProgramBusinessCaseClient } from "../Meros/Meros.PortfolioExt/ProgramBusinessCase/ProgramBusinessCaseClient"
import { ProgramCharterClient } from "../Meros/Meros.PortfolioExt/ProgramCharter/ProgramCharterClient"
import { ResponsibilitiesClient } from "../Meros/Meros.PortfolioExt/Responsibilities/ResponsibilitiesClient"
import { StatusReportExtraClient } from "../Meros/Meros.PortfolioExt/StatusReportExtra/StatusReportExtraClient"
import { LessonsLearnedClient } from "../Meros/Meros.PortfolioExt/LessonsLearned/LessonsLearnedClient"
import { DomainLabelClient } from "../Meros/Meros.PortfolioExt/DomainLabel/DomainLabelClient"

import { RRSPProjectClient } from "./Project/RRSPProjectClient"
import { TourClient } from "@extensions/Signum.Tour/TourClient"
import { RRSPProgramClient } from "./Program/RRSPProgramClient"
import { RRSPPortfolioClient } from "./Portfolio/RRSPPortfolioClient"

export function startFull(routes: RouteObject[]): void {
  Operations.start();
  Navigator.start({ routes });
  Finder.start({ routes });
  Finder.Options.tokenCanSetPropery = qt => (qt.filterType == "Lite" || Boolean(qt.propertyRoute && PropertyRoute.tryParseFull(qt.propertyRoute)?.member?.type.name == "Date")) && qt.key != "Entity";
  QuickLinkClient.start();
  VisualTipClient.start({ routes });
  ChangeLogClient.start({ routes, applicationName:"RRSP", mainChangeLog: () => import("./Changelog") });

  AuthAdminClient.start({ routes, types: true, properties: true, operations: true, queries: "queryContext", permissions: true });
  //AzureADClient.start({ routes, adGroups: false,  profilePhotos: true, inviteUsers: true });

  ExceptionClient.start({ routes });
  TourClient.start({ routes });

  FilesClient.start({ routes });
  UserQueryClient.start({ routes });
  CacheClient.start({ routes });
  ProcessClient.start({ routes, packages: true, packageOperations: true });
  MailingClient.start({ routes, contextual: true, queryButton: true });
  WordClient.start({ routes, contextual: false, queryButton: true, entityButton: false });
  ExcelClient.start({ routes, plainExcel: true, excelReport: true, importFromExcel: true});
  SchedulerClient.start({ routes });
  TranslationClient.start({ routes });
  TranslatedInstanceClient.start({ routes });
  DiffLogClient.start({ routes});
  ProfilerClient.start({ routes });
  TimeMachineClient.start({ routes });
  ChartClient.start({ routes });
  DashboardClient.start({ routes });
  AlertsClient.start({
    routes,
    showAlerts: (t, when) => when == "QuickLink" && [
      TaskEntity.typeName,
      ProjectEntity.typeName
    ].contains(t)
  });
  MapClient.start({ routes });
  ToolbarClient.start({ routes });
  SubsClient.start({ routes });
  ConcurrentUserClient.start({ routes });
  OmniboxClient.start();
  WhatsNewClient.start({ routes });
  HelpClient.start({ routes});

  ProjectClient.start({ routes });
  PortfolioClient.start({ routes });
  ProgramClient.start({ routes });
  TasksClient.start({ routes });
  ProtocolClient.start({ routes });
  DecisionClient.start({ routes });
  VideoClient.start({ routes });
  StatusReportClient.start({ routes, hasBilling: false });
  StakeholderClient.start({ routes });
  RiskClient.start({ routes });
  FinancialsClient.start({ routes });
  PSCClient.start({ routes });
  WorkPackageClient.start({ routes });
  TaskManagementClient.start({ routes });
  BusinessCaseClient.start({ routes });
  BaseClient.start({ routes });
  MyProjectsClient.start({ routes });
  MyProgramsClient.start({ routes });
  MyPortfoliosClient.start({ routes });
  RASCIClient.start({ routes });
  PortfolioExtClient.start({ routes });
  InitiationRequestClient.start({ routes });
  ProjectPrioritizationClient.start({ routes });
  PlanOverviewClient.start({ routes });
  PortfolioRelationshipClient.start({ routes });
  ProgramBusinessCaseClient.start({ routes });
  ProgramCharterClient.start({ routes });
  ResponsibilitiesClient.start({ routes });
  StatusReportExtraClient.start({ routes });
  LessonsLearnedClient.start({ routes });
  DomainLabelClient.start({ routes });
  StatementOfWorkClient.start({ routes });
  GlobalsClient.start({ routes });
  RRSPProjectClient.start({ routes });
  RRSPProgramClient.start();
  RRSPPortfolioClient.start();
}
