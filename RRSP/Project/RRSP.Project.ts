//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Framework/Signum/React/Reflection'
import * as Entities from '../../Framework/Signum/React/Signum.Entities'
import * as Operations from '../../Framework/Signum/React/Signum.Operations'
import * as Authorization from '../../Framework/Extensions/Signum.Authorization/Signum.Authorization'
import * as Risk from '../../Meros/Meros.Risk/Meros.Risk'
import * as LessonsLearned from '../../Meros/Meros.PortfolioExt/LessonsLearned/Meros.PortfolioExt.LessonsLearned'
import * as Tasks from '../../Meros/Meros.Tasks/Meros.Tasks'
import * as Project from '../../Meros/Meros.Project/Meros.Project'
import * as Stakeholder from '../../Meros/Meros.Stakeholder/Meros.Stakeholder'


export const ColumnAction: EnumType<ColumnAction> = new EnumType<ColumnAction>("ColumnAction");
export type ColumnAction =
  "Move" |
  "Copy" |
  "Replace" |
  "Ignore";

export const ColumnActionEmbedded: Type<ColumnActionEmbedded> = new Type<ColumnActionEmbedded>("ColumnActionEmbedded");
export interface ColumnActionEmbedded extends Entities.EmbeddedEntity {
  Type: "ColumnActionEmbedded";
  column: Entities.Lite<Tasks.ColumnEntity>;
  action: ColumnAction;
  targetColumn: Entities.Lite<Tasks.ColumnEntity> | null;
}

export namespace DashboardTemplateMessage {
  export const NumberOfProjects: MessageKey = new MessageKey("DashboardTemplateMessage", "NumberOfProjects");
  export const Progress: MessageKey = new MessageKey("DashboardTemplateMessage", "Progress");
  export const ProjectOwner: MessageKey = new MessageKey("DashboardTemplateMessage", "ProjectOwner");
  export const Runtime: MessageKey = new MessageKey("DashboardTemplateMessage", "Runtime");
  export const MonitoringAndControl: MessageKey = new MessageKey("DashboardTemplateMessage", "MonitoringAndControl");
  export const ContinuousProjectMonitoringActive: MessageKey = new MessageKey("DashboardTemplateMessage", "ContinuousProjectMonitoringActive");
  export const NoMonitoring: MessageKey = new MessageKey("DashboardTemplateMessage", "NoMonitoring");
  export const ProjectProgressWeighted: MessageKey = new MessageKey("DashboardTemplateMessage", "ProjectProgressWeighted");
  export const NoStandardPlan: MessageKey = new MessageKey("DashboardTemplateMessage", "NoStandardPlan");
  export const _0Of1WorkPackagesCompleted: MessageKey = new MessageKey("DashboardTemplateMessage", "_0Of1WorkPackagesCompleted");
  export const KanbanBoard: MessageKey = new MessageKey("DashboardTemplateMessage", "KanbanBoard");
  export const Of0TotalBudget: MessageKey = new MessageKey("DashboardTemplateMessage", "Of0TotalBudget");
  export const BudgetExhausted: MessageKey = new MessageKey("DashboardTemplateMessage", "BudgetExhausted");
  export const Available: MessageKey = new MessageKey("DashboardTemplateMessage", "Available");
}

export const LabelAction: EnumType<LabelAction> = new EnumType<LabelAction>("LabelAction");
export type LabelAction =
  "Move" |
  "Copy" |
  "Replace" |
  "Ignore";

export const LabelActionEmbedded: Type<LabelActionEmbedded> = new Type<LabelActionEmbedded>("LabelActionEmbedded");
export interface LabelActionEmbedded extends Entities.EmbeddedEntity {
  Type: "LabelActionEmbedded";
  label: Entities.Lite<Tasks.TaskLabelEntity>;
  action: LabelAction;
  targetLabel: Entities.Lite<Tasks.TaskLabelEntity> | null;
}

export const NewBoardEmbedded: Type<NewBoardEmbedded> = new Type<NewBoardEmbedded>("NewBoardEmbedded");
export interface NewBoardEmbedded extends Entities.EmbeddedEntity {
  Type: "NewBoardEmbedded";
  columns: Entities.MList<NewColumnEmbedded>;
}

export const NewColumnEmbedded: Type<NewColumnEmbedded> = new Type<NewColumnEmbedded>("NewColumnEmbedded");
export interface NewColumnEmbedded extends Entities.EmbeddedEntity {
  Type: "NewColumnEmbedded";
  name: string;
  taskState: Tasks.TaskState;
  archiveTasksOlderThan: number | null;
}

export const NewProjectModel: Type<NewProjectModel> = new Type<NewProjectModel>("NewProjectModel");
export interface NewProjectModel extends Entities.ModelEntity {
  Type: "NewProjectModel";
  projectName: string;
  projectManager: Entities.Lite<Authorization.UserEntity>;
  projectPrefix: string;
  members: Entities.MList<Entities.Lite<Authorization.UserEntity>>;
  createNewBoard: NewBoardEmbedded | null;
  riskManagement: Risk.RiskManagementEmbedded | null;
  createTasksFromLessonsLearned: Entities.MList<Entities.Lite<LessonsLearned.LessonsLearnedEntity>>;
}

export namespace ProjectExpandedOperation {
  export const Create : Operations.ConstructSymbol_Simple<Project.ProjectEntity> = registerSymbol("Operation", "ProjectExpandedOperation.Create");
  export const Split : Operations.ExecuteSymbol<Project.ProjectEntity> = registerSymbol("Operation", "ProjectExpandedOperation.Split");
}

export namespace ProjectExtensionMessage {
  export const Basic: MessageKey = new MessageKey("ProjectExtensionMessage", "Basic");
  export const Options: MessageKey = new MessageKey("ProjectExtensionMessage", "Options");
}

export const ProjectSplitModel: Type<ProjectSplitModel> = new Type<ProjectSplitModel>("ProjectSplitModel");
export interface ProjectSplitModel extends Entities.ModelEntity {
  Type: "ProjectSplitModel";
  sourceProject: Entities.Lite<Project.ProjectEntity>;
  targetProject: Entities.Lite<Project.ProjectEntity>;
  tasks: Entities.MList<TaskActionEmbedded>;
  labels: Entities.MList<LabelActionEmbedded>;
  columns: Entities.MList<ColumnActionEmbedded>;
}

export const TaskActionEmbedded: Type<TaskActionEmbedded> = new Type<TaskActionEmbedded>("TaskActionEmbedded");
export interface TaskActionEmbedded extends Entities.EmbeddedEntity {
  Type: "TaskActionEmbedded";
  task: Entities.Lite<Tasks.TaskEntity>;
}

export const TaskRiskMixin: Type<TaskRiskMixin> = new Type<TaskRiskMixin>("TaskRiskMixin");
export interface TaskRiskMixin extends Entities.MixinEntity {
  Type: "TaskRiskMixin";
  risks: Entities.MList<Entities.Lite<Risk.RiskEntity>>;
}

export const TaskStakeholderMixin: Type<TaskStakeholderMixin> = new Type<TaskStakeholderMixin>("TaskStakeholderMixin");
export interface TaskStakeholderMixin extends Entities.MixinEntity {
  Type: "TaskStakeholderMixin";
  stakeholders: Entities.MList<Entities.Lite<Stakeholder.StakeholderEntity>>;
}

