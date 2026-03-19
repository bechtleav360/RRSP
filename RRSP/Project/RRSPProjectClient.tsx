import { RouteObject } from 'react-router'
import { EntitySettings, Navigator } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { Operations, EntityOperationContext, ConstructorOperationSettings, EntityOperationSettings, EntityOperationGroup } from '@framework/Operations'
import * as AppContext from '@framework/AppContext'
import { EntityControlMessage, EntityPack, Lite, SearchMessage, getMixin, getToString, newMListElement, toLite } from '@framework/Signum.Entities'
import { AutoLine, CheckboxLine, EntityDetail, EntityStrip, TypeContext } from '@framework/Lines'
import { SearchControl, SearchValueLine } from '@framework/Search';
import { DomainRiskMixin, RiskEntity, RiskManagementEmbedded, RiskTemplateEntity } from '../../Meros/Meros.Risk/Meros.Risk'
import { MeetingProtocolEntity, DomainProtocolMixin, ProtocolTemplateEntity } from '../../Meros/Meros.Protocol/Meros.Protocol'
import { AccessLevel, ProjectEntity, MemberEntity, ProjectMessage, ExternalMemberEntity } from '../../Meros/Meros.Project/Meros.Project'
import { DomainStakeholderMixin, StakeholderTemplateEntity, StakeholderEntity } from '../../Meros/Meros.Stakeholder/Meros.Stakeholder'
import { useAPI } from '@framework/Hooks'
import { OrderProjectEntity } from '../../Meros/Meros.Project/Orders/Meros.Project.Orders'
import { DomainTaskMixin, TaskEntity, TaskMessage, TaskOperation } from '../../Meros/Meros.Tasks/Meros.Tasks'
import ErrorModal from '@framework/Modals/ErrorModal'
import { AuthClient } from '@extensions/Signum.Authorization/AuthClient'
import { ProjectClient } from '../../Meros/Meros.Project/ProjectClient'
import { DomainTask, DomainTaskOption } from '../../Meros/Meros.Tasks/Templates/DomainTask'
import ProjectProtocol from '../../Meros/Meros.Protocol/Templates/ProjectProtocol'
import RiskSettings from '../../Meros/Meros.Risk/Templates/RiskSettings'
import ProjectStatusReport from '../../Meros/Meros.StatusReport/Templates/ProjectStatusReport'
import StakeholderMixin from '../../Meros/Meros.Stakeholder/Templates/StakeholderMixin'
import StakeholderMatrix from '../../Meros/Meros.Stakeholder/Templates/StakeholderMatrix'
import ProjectEmailTemplate from './Templates/ProjectEmailTemplate'
import { StatusReportEntity, StatusReportTemplateEntity } from '../../Meros/Meros.StatusReport/Meros.StatusReport'
import { ChangeRequestTemplateEntity } from '../../Meros/Meros.PlanningProject/PSC/Meros.PlanningProject.PSC'
import { ProjectPlans } from '../../Meros/Meros.PlanningProject/WorkPackage/Templates/ProjectPlans'
import { ProjectWorkPackageTemplateEntity } from '../../Meros/Meros.PlanningProject/WorkPackage/Meros.PlanningProject.WorkPackage'
import { BusinessCaseEntity, CharterEntity } from '../../Meros/Meros.PlanningProject/BusinessCase/Meros.PlanningProject.BusinessCase';
import ProjectBusinessCase from '../../Meros/Meros.PlanningProject/BusinessCase/Templates/ProjectBusinessCase';
import ProjectProjectCharter from '../../Meros/Meros.PlanningProject/BusinessCase/Templates/ProjectProjectCharter';
import { EntityOperations, OperationButton } from '../../Framework/Signum/React/Operations/EntityOperations'
import { Tab } from 'react-bootstrap';
import { UserEntity } from '../../Framework/Extensions/Signum.Authorization/Signum.Authorization'
import { DashboardClient } from '../../Framework/Extensions/Signum.Dashboard/DashboardClient'
import { NewBoardEmbedded, NewColumnEmbedded, NewProjectModel, ProjectExpandedOperation, ProjectExtensionMessage, ProjectSplitModel, TaskActionEmbedded, TaskRiskMixin, TaskStakeholderMixin } from './RRSP.Project'
import { reloadTypesInDomains, typeAllowedInDomain } from '@framework/Reflection'
import { InitiationRequestEntity, InitiationRequestOperation, InitiationRequestPropertyEntity } from '../../Meros/Meros.PortfolioExt/InitiationRequest/Meros.PortfolioExt.InitiationRequest'
import ProjectRasciMatrix from '../../Meros/Meros.PlanningProject/RASCI/ProjectRasciMatrix'
import { RASCIMatrixEntity } from '../../Meros/Meros.PlanningProject/RASCI/Meros.PlanningProject.RASCI'
import { Constructor } from '../../Framework/Signum/React/Constructor'
import MessageModal from '@framework/Modals/MessageModal'
import { ImportComponent } from '../../Framework/Signum/React/ImportComponent'
import Portfolio from '../../Meros/Meros.Project/Portfolio/Templates/Portfolio'
import { DomainAssignedLabelEntity } from '../../Meros/Meros.PortfolioExt/DomainLabel/Meros.PortfolioExt.DomainLabel'
import { DomainLabelsEditor } from '../../Meros/Meros.PortfolioExt/DomainLabel/Templates/DomainLabel'
import { ProgramEntity } from '../../Meros/Meros.Project/Program/Meros.Project.Program'
import { PortfolioEntity, ProjectPortfolioMixin } from '../../Meros/Meros.Project/Portfolio/Meros.Project.Portfolio'
import { DashboardEntity } from '../../Framework/Extensions/Signum.Dashboard/Signum.Dashboard'
import { ajaxGet } from '../../Framework/Signum/React/Services'

export namespace RRSPProjectClient {

  export function start(options: { routes: RouteObject[] }): void {

    options.routes.push({ path: "/CommunicationManagement/:domainKey", element: <ImportComponent onImport={() => import("./Templates/CommunicationManagementPage")} /> });

    Navigator.addSettings(new EntitySettings(ProjectSplitModel, p => import('./Templates/ProjectSplit')));
    Navigator.addSettings(new EntitySettings(NewProjectModel, p => import('./Templates/NewProject')));

    if (CharterEntity.tryTypeInfo() != null) {
      ProjectClient.addProjectItem('Initiation', {
        order: 1, title: CharterEntity.niceName(), icon: "file-contract", component: ctx => <ProjectProjectCharter ctx={ctx} />
      })
    }

    DashboardClient.Options.registerCustomPartRenderer(ProjectEntity, "Main", () => import("./DashboardTemplates/ProjectMainPart"));

    DashboardClient.Options.registerCustomPartRenderer(ProjectEntity, "GoalPart", () => import("./DashboardTemplates/GoalPart"), { withPanel: false });
    DashboardClient.Options.registerCustomPartRenderer(ProjectEntity, "PlanWithMilestonePart", () => import("./DashboardTemplates/PlanWithMilestonePart"), { withPanel: true });
    DashboardClient.Options.registerCustomPartRenderer(ProjectEntity, "MilestonePart", () => import("./DashboardTemplates/MilestonePart"), { withPanel: false });

    function splitProject(project: Lite<ProjectEntity>, model: ProjectSplitModel): Promise<EntityPack<ProjectEntity> | undefined> {
      return Navigator.view(model, { modalSize: "xl" })
        .then(split => {
          if (!split)
            return undefined;

          return Operations.API.executeLite(project, ProjectExpandedOperation.Split, split)
            .then(pack => {
              Operations.notifySuccess();
              return showNewProject(split).then(() => pack);
            })
            .catch(e => ErrorModal.showErrorModal(e).then(() => splitProject(project, split)));
        });
    }

    function migrateTask(eoc: EntityOperationContext<TaskEntity>, targetProject: Lite<ProjectEntity>): Promise<void> {

      var model = ProjectSplitModel.New({
        sourceProject: eoc.entity.domain as Lite<ProjectEntity>,
        targetProject: targetProject,
        tasks: [newMListElement(TaskActionEmbedded.New({ task: toLite(eoc.entity) }))]
      });

      return splitProject(eoc.entity.domain as Lite<ProjectEntity>, model)
        .then(projectPack => Navigator.API.fetchEntityPack(toLite(eoc.entity)))
        .then(taskPack => eoc.frame.onReload(taskPack));
    }
    var migrateGroup: EntityOperationGroup = { key: "Migrate", text: () => TaskMessage.Migrate.niceToString() };

    Operations.addSettings(new EntityOperationSettings(TaskOperation.MigrateToExistingProject, {
      hideOnCanExecute: true,
      isVisible: eoc => ProjectEntity.isLite(eoc.entity.domain),
      group: migrateGroup,
      onClick: eoc => Finder.find<ProjectEntity>({
        queryName: ProjectEntity,
        filterOptions: [{ token: ProjectEntity.token(a => a.entity), operation: "DistinctTo", value: eoc.entity.domain }]
      })
        .then(pro => pro && migrateTask(eoc, pro)),
    }));

    Operations.addSettings(new EntityOperationSettings(ProjectExpandedOperation.Split, {
      onClick: eoc => splitProject(toLite(eoc.entity), ProjectSplitModel.New({ sourceProject: toLite(eoc.entity) }))
        .then(pack => eoc.frame.onReload(pack))
    }));

    function showNewProject(split: ProjectSplitModel): Promise<any> {
      return MessageModal.show({
        style: "success",
        title: ProjectMessage.ProjectSplitSuccesfullyExecuted.niceToString(),
        message: <div>
          <p>{ProjectMessage.AllTheSelectedEntitiesHaveBeenSucesfullyMigrated.niceToString()}</p>
          <p>{ProjectMessage.DoYouWantToOpenTheTargetProject0.niceToString(getToString(split.targetProject))}</p>
        </div>,
        buttons: "yes_no",
      }).then(b => b != "yes" ? undefined : Navigator.view(split.targetProject));
    }

    Operations.addSettings(new ConstructorOperationSettings(ProjectExpandedOperation.Create, {
      onConstruct: coc => {
        const viewAndExecute = (model: NewProjectModel): Promise<EntityPack<ProjectEntity> | undefined> => {
          return Navigator.view(model)
            .then(m => m && coc.defaultConstruct(m)
              .then(p => { reloadTypesInDomains(); return p; }, 
                e => ErrorModal.showErrorModal(e).then(() => viewAndExecute(m))));
        };

        return viewAndExecute(NewProjectModel.New({
          members: [newMListElement(toLite(AuthClient.currentUser()))],
          projectPrefix: "TFS",
          riskManagement: RiskManagementEmbedded.New(),
          createNewBoard: NewBoardEmbedded.New({
            columns: [
              newMListElement(NewColumnEmbedded.New({ name: "To Do", taskState: 'Open' })),
              newMListElement(NewColumnEmbedded.New({ name: "In Progress", taskState: 'InProgress' })),
              newMListElement(NewColumnEmbedded.New({ name: "Done", taskState: 'Done' })),
            ]
          }),
        }));
      }
    }));

    if (BusinessCaseEntity.tryTypeInfo() != null) {
      ProjectClient.addProjectItem('Initiation', {
        order: 2, title: BusinessCaseEntity.niceName(), icon: "circle-info", component: ctx => <ProjectBusinessCase ctx={ctx} />
      })
    }

    if (ProjectEntity.tryTypeInfo()) {
      //ProjectClient.addProjectItem('Planning', {
      //  order: 5, title: PSCMessage.ScopeCatalogue.niceToString(), icon: "bullseye-arrow", component: ctx => <button className='btn btn-sm btn-outline-secondary'
      //    onClick={e => {
      //      e.preventDefault();
      //      AppContext.navigate(`/PSCPage/${ctx.value.id}`)
      //    }}
      //  >
      //    {PSCMessage.ScopeCatalogue.niceToString()}
      //  </button>
      //});

      ProjectClient.addProjectItem('Planning', {
        order: 6, title: ProjectMessage.Plans.niceToString(), icon: "chart-gantt", component: ctx => <ProjectPlans ctx={ctx} />
      });

      ProjectClient.addProjectItem("Execution",
        { order: 1, title: ProjectExtensionMessage.Basic.niceToString(), icon: 'circle-info', component: ctx => <ProjectExecutionBasic ctx={ctx} /> });
    
      if (StakeholderEntity.tryTypeInfo()) {
        ProjectClient.addProjectItem("Execution",
          { order: 5, title: StakeholderEntity.nicePluralName(), icon: 'users', component: ctx => <StakeholderMatrix ctx={ctx} /> });
      }
        
      ProjectClient.addProjectItem("Configuration", {
        order: 1, title: ProjectExtensionMessage.Options.niceToString(), icon: 'list-check', component: ctx => <ProjectConfigurationOptions ctx={ctx} />
      });


      ProjectClient.addProjectItem("Configuration", {
        order: 5, title: ProjectEntity.mixinNicePropertyName(DomainProtocolMixin, p => p.emailTemplate), icon: 'envelope', component: ctx => <ProjectEmailTemplate ctx={ctx.subCtx({ labelColumns: 4 })} />
      });


      ProjectClient.addProjectItem("Configuration", {
        order: 7, title: ProjectEntity.mixinNicePropertyName(DomainRiskMixin, p => p.riskManagement), icon: 'triangle-exclamation',
        component: ctx => <RiskSettings ctx={ctx.subCtx({ labelColumns: 4 })} />
      });

      ProjectClient.addProjectItem("Configuration", {
        order: 8, title: ProjectEntity.mixinNicePropertyName(DomainStakeholderMixin, p => p.stakeholderStrategies), icon: 'users',
        component: ctx => <StakeholderMixin ctx={ctx.subCtx({ labelColumns: 4 })} />
      });

      //ProjectClient.addProjectItem("Financials", {
      //  order: 1, title: CustomerEntity.nicePluralName(), icon: 'building', component: ctx =>
      //    <SearchControl ctx={ctx} deps={[ctx.frame?.refreshCount]}
      //    findOptions={{ queryName: CustomerEntity, filterOptions: [{ token: CustomerEntity.token(e => e.entity).expression<ProjectEntity[]>("Domains").any(), value: ctx.value }] }}
      //  />
      //});
    }

    ProjectClient.Options.ProjectMemberCheckbox = (ctx) => {
      return <>
        <CheckboxLine ctx={ctx.subCtx(DomainTaskMixin).subCtx(p => p.addNewUsersAutomatically)} inlineCheckbox="block" />
      </>;
    };   

    Navigator.getSettings(TaskEntity)?.overrideView(vr => {
      vr.insertAfterLine(a => a.createdFrom, a => [
        <EntityStrip vertical ctx={vr.ctx.subCtx(TaskStakeholderMixin).subCtx(m => m.stakeholders, { formGroupStyle: 'Basic' })} findOptions={{
          queryName: StakeholderEntity,
          filterOptions: [{ token: StakeholderEntity.token(a => a.domain), value: vr.ctx.value.domain }]
        }
        } />,
        <EntityStrip vertical ctx={vr.ctx.subCtx(TaskRiskMixin).subCtx(m => m.risks, { formGroupStyle: 'Basic' })} findOptions={{
          queryName: RiskEntity,
          filterOptions: [{ token: RiskEntity.token(a => a.domain), value: vr.ctx.value.domain }]
        }
        } />
      ]);
      vr.removeLine(a => a.effort);
    });

    Navigator.getSettings(StakeholderEntity)?.overrideView(vr => {
      !vr.ctx.value.isNew &&
        vr.addTab("appTabs", <Tab eventKey="Tasks" title={TaskEntity.nicePluralName()}>
          <SearchControl
            onCreate={async sc => {
              var pack = await Constructor.constructPack(TaskEntity, { domain: vr.ctx.value.domain });
              getMixin(pack!.entity, TaskStakeholderMixin).stakeholders.push(newMListElement(toLite(vr.ctx.value)));
              return await Navigator.view(pack!);
            }}
            findOptions={{
              queryName: TaskEntity, filterOptions: [{
                token: TaskEntity.token(a => a.entity).mixin(TaskStakeholderMixin).append(a => a.stakeholders).any(),
                value: vr.ctx.value
              }],
              columnOptionsMode: 'Remove',
              columnOptions: [
                { token: TaskEntity.token(s => s.column) },
                { token: TaskEntity.token(s => s.domain) },
              ],  
            }} />
        </Tab>);
    })

    Navigator.getSettings(RiskEntity)?.overrideView(vr => {
      !vr.ctx.value.isNew &&
        vr.addTab("appTabs", <Tab eventKey="Tasks" title={TaskEntity.nicePluralName()}>
          <SearchControl
            onCreate={async sc => {
              var pack = await Constructor.constructPack(TaskEntity, { domain: vr.ctx.value.domain });
              getMixin(pack!.entity, TaskRiskMixin).risks.push(newMListElement(toLite(vr.ctx.value)));
              return await Navigator.view(pack!);
            }}
            findOptions={{
              queryName: TaskEntity, filterOptions: [{
                token: TaskEntity.token(a => a.entity).mixin(TaskRiskMixin).append(a => a.risks).any(),
                value: vr.ctx.value
              }]
            }} />
        </Tab>);
    })

    Navigator.getSettings(ProjectEntity)!.overrideView(vr => {
      var ctxBasic = vr.ctx.subCtx({ formGroupStyle: "Basic" });

      vr.replaceLine(p => p.state, l => [
        <div className="row">
          <div className="col-sm-6">
            <AutoLine ctx={ctxBasic.subCtx(DomainTaskMixin).subCtx(p => p.prefix)} readOnly={vr.ctx.value.state == "Archived"} />
          </div>
          <div className="col-sm-6">
            {l}
          </div>
        </div>]);

      vr.replaceLine(p => p.defaultDashboard, l => [
        <div className="row">
          <div className="col-sm-4">
            {l}
          </div>
          <div className="col-sm-8">
            {!vr.ctx.value.isNew && <DomainLabelsEditor ctx={vr.ctx.subCtx({ formGroupStyle: 'Basic' })} />}
          </div>
        </div>]);

      vr.insertAfterLine(p => p.name, l => [
        <div className="row">
          <div className="col-sm-6">
            <AutoLine ctx={ctxBasic.subCtx(ProjectPortfolioMixin).subCtx(p => p.parentDomain)} readOnly />
          </div>
          <div className="col-sm-6">
            {!vr.ctx.value.isNew && <SearchValueLine ctx={ctxBasic} findOptions={{
              queryName: InitiationRequestEntity,
              filterOptions: [{ token: InitiationRequestEntity.token(a => a.fromDomain), value: vr.ctx.value }]
            }} extraButtons={c => <OperationButton size={"xs" as "sm"} className="ms-3"  eoc={EntityOperationContext.fromTypeContext(vr.ctx, InitiationRequestOperation.CreateInitiationRequestFromProject)}>
              {EntityOperations.withIcon(EntityControlMessage.Create.niceToString(), "code-merge")}
              </OperationButton>} />
            }
          </div>
        </div>
      ]);
    });

  }

  export namespace API {

  }

  function ProjectExecutionBasic(p: { ctx: TypeContext<ProjectEntity> }) {
    var ctx5 = p.ctx.subCtx({ labelColumns: 5 });

    if (p.ctx.value.isNew)
      return null;

    return (<div style={{ overflowX: 'hidden' }}>
      <div className="row">
        <div className="col-sm-4">
          <DomainTask ctx={ctx5} />
        </div>
        <div className="col-sm-4">
          {MeetingProtocolEntity.tryTypeInfo() && <ProjectProtocol ctx={ctx5} />}

          {StatusReportEntity.tryTypeInfo() && <div className="mt-1">
            <ProjectStatusReport ctx={ctx5} />
          </div>}
          {RASCIMatrixEntity.tryTypeInfo() && <div className="mt-1">
            <ProjectRasciMatrix ctx={ctx5} />
          </div>}
        </div>
      </div>
    </div>);
  }

  function ProjectConfigurationOptions(p: { ctx: TypeContext<ProjectEntity> }) {
    var ctx = p.ctx;
    var ctx6 = ctx.subCtx({ labelColumns: 6 });
    var ctx4 = ctx.subCtx({ labelColumns: 4 });

    var hasMeetingProtocol = useAPI(() => MeetingProtocolEntity.tryTypeInfo() == null ? Promise.resolve(null) :
      Finder.inDB<number>(ctx.value, ProjectEntity.token(p => p.entity).expression("MeetingProtocols").count()).then(val => val), [ctx.frame?.refreshCount], { avoidReset: true });

    if (ctx.value.isNew)
      return null;

    return (
      <div style={{ overflowX: 'hidden' }}>
        <div className="row">
          <div className="col-sm-8">
            <DomainTaskOption ctx={ctx4} />
          </div>
          <div className="col-sm-4">
            <SearchValueLine ctx={ctx6} deps={[ctx.frame?.refreshCount]} create={typeAllowedInDomain(StatusReportTemplateEntity, toLite(ctx.value), true)}
              findOptions={{ queryName: StatusReportTemplateEntity, filterOptions: [{ token: StatusReportTemplateEntity.token(a => a.domain), value: ctx.value }] }}
              onExplored={() => ctx.frame?.frameComponent.forceUpdate()}
            />

            <SearchValueLine ctx={ctx6} deps={[ctx.frame?.refreshCount]} create={typeAllowedInDomain(ProtocolTemplateEntity, toLite(ctx.value), true)}
              findOptions={{ queryName: ProtocolTemplateEntity, filterOptions: [{ token: ProtocolTemplateEntity.token(a => a.domain), value: ctx.value }] }}
              onExplored={() => ctx.frame?.frameComponent.forceUpdate()}
            />

            <SearchValueLine ctx={ctx6} deps={[ctx.frame?.refreshCount]} create={typeAllowedInDomain(RiskTemplateEntity, toLite(ctx.value), true)}
              findOptions={{ queryName: RiskTemplateEntity, filterOptions: [{ token: RiskTemplateEntity.token(a => a.domain), value: ctx.value }] }}
              onExplored={() => ctx.frame?.frameComponent.forceUpdate()}
            />

            <SearchValueLine ctx={ctx6} deps={[ctx.frame?.refreshCount]} create={typeAllowedInDomain(StakeholderTemplateEntity, toLite(ctx.value), true)}
              findOptions={{ queryName: StakeholderTemplateEntity, filterOptions: [{ token: StakeholderTemplateEntity.token(a => a.domain), value: ctx.value }] }}
              onExplored={() => ctx.frame?.frameComponent.forceUpdate()}
            />

            <SearchValueLine ctx={ctx6} deps={[ctx.frame?.refreshCount]} create={typeAllowedInDomain(ChangeRequestTemplateEntity, toLite(ctx.value), true)}
              findOptions={{ queryName: ChangeRequestTemplateEntity, filterOptions: [{ token: ChangeRequestTemplateEntity.token(a => a.project), value: ctx.value }] }}
              onExplored={() => ctx.frame?.frameComponent.forceUpdate()}
            />

            <SearchValueLine ctx={ctx6} deps={[ctx.frame?.refreshCount]} create={typeAllowedInDomain(ProjectWorkPackageTemplateEntity, toLite(ctx.value), true)}
              findOptions={{ queryName: ProjectWorkPackageTemplateEntity, filterOptions: [{ token: ProjectWorkPackageTemplateEntity.token(a => a.project), value: ctx.value }] }}
              onExplored={() => ctx.frame?.frameComponent.forceUpdate()}
            />
          </div>
        </div>
      </div>);
  }
}


ProjectClient.ProjectExecutionProjectMember = (p: { ctx: TypeContext<ProjectEntity> }) => {
  var ctx = p.ctx;

  if (ctx.value.isNew)
    return null;

  return <div style={{ overflowX: 'hidden' }}>
    <SearchControl findOptions={{
      queryName: MemberEntity,
      filterOptions: [
        { token: MemberEntity.token(a => a.domain), value: ctx.value },
        {
          token: MemberEntity.token(a => a.entity.user).getToString(),
          operation: "Contains",
          value: null,
          pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
        }
      ],
    }}
      onCreate={() =>
        Finder.find<UserEntity>({
          queryName: UserEntity,
          filterOptions: [{
            token: UserEntity.token(a => a.entity).expression("Domains").notAny(),
            value: ctx.value,
          }]
        }).then(u => Navigator.view(MemberEntity.New({
          domain: toLite(ctx.value),
          user: u,
          dontDeleteOnSync: ProjectClient.Options.HasSyncIntegration(ctx.value)
        })))
      } />
    {ProjectClient.Options.ProjectMemberCheckbox?.(ctx)}
    <hr />
    <h4>{ProjectMessage.ExternalProjectMembersForMeetingProtocols.niceToString()} </h4>
    <SearchControl findOptions={{
      queryName: ExternalMemberEntity,
      filterOptions: [
        { token: ExternalMemberEntity.token(a => a.domain), value: ctx.value, frozen: true },
        {
          token: ExternalMemberEntity.token(a => a.entity).getToString(),
          operation: "Contains",
          value: null,
          pinned: { label: SearchMessage.Search.niceToString(), splitValue: true, active: "WhenHasValue" },
        }
      ],
    }}
      create={typeAllowedInDomain(ExternalMemberEntity, toLite(ctx.value), true)}
    />
  </div>;
}
