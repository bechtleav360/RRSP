import { EntitySettings, Navigator } from '@framework/Navigator'
import { Operations, ConstructorOperationSettings, EntityOperationContext } from '@framework/Operations'
import { EntityPack } from '@framework/Signum.Entities'
import { NewProgramModel, ProgramExpandedOperation } from './RRSP.Program'
import { ProgramEntity } from '../../Meros/Meros.Project/Program/Meros.Project.Program'
import { RiskManagementEmbedded, DomainRiskMixin } from '../../Meros/Meros.Risk/Meros.Risk'
import { DashboardClient } from '@extensions/Signum.Dashboard/DashboardClient'
import { DomainTaskMixin } from '../../Meros/Meros.Tasks/Meros.Tasks'
import { InitiationRequestEntity, InitiationRequestOperation, InitiationRequestPropertyEntity } from '../../Meros/Meros.PortfolioExt/InitiationRequest/Meros.PortfolioExt.InitiationRequest'
import { DomainStakeholderMixin, StakeholderEntity } from '../../Meros/Meros.Stakeholder/Meros.Stakeholder'
import { DomainProtocolMixin } from '../../Meros/Meros.Protocol/Meros.Protocol'
import { DomainLabelsEditor } from '../../Meros/Meros.PortfolioExt/DomainLabel/Templates/DomainLabel'
import { AutoLine, EntityDetail } from '@framework/Lines'
import { SearchControl, SearchValueLine } from '@framework/Search'
import { Tab } from 'react-bootstrap'
import { EntityOperations, OperationButton } from '@framework/Operations/EntityOperations'
import { EntityControlMessage } from '@framework/Signum.Entities'
import ProjectEmailTemplate from '../Project/Templates/ProjectEmailTemplate'
import ErrorModal from '@framework/Modals/ErrorModal'
import { reloadTypesInDomains } from '@framework/Reflection'

export namespace RRSPProgramClient {

  export function start(): void {

    Navigator.addSettings(new EntitySettings(NewProgramModel, p => import('./Templates/NewProgram')));

    DashboardClient.Options.registerCustomPartRenderer(ProgramEntity, "GoalPart", () => import("../Project/DashboardTemplates/GoalPart"), { withPanel: false });
    DashboardClient.Options.registerCustomPartRenderer(ProgramEntity, "PlanWithMilestonePart", () => import("../Project/DashboardTemplates/PlanWithMilestonePart"), { withPanel: true });
    DashboardClient.Options.registerCustomPartRenderer(ProgramEntity, "MilestonePart", () => import("../Project/DashboardTemplates/MilestonePart"), { withPanel: false });

    Navigator.getSettings(ProgramEntity)!.overrideView(vr => {
      var ctxBasic = vr.ctx.subCtx({ formGroupStyle: "Basic" });
      vr.replaceLine(p => p.manager, man => [
        <div className="row">
          <div className="col-sm-8">
            {man}
          </div>
          <div className="col-sm-4">
            <AutoLine ctx={ctxBasic.subCtx(DomainTaskMixin).subCtx(p => p.prefix)} />
          </div>
        </div>]);


      vr.replaceLine(p => p.portfolio, port => [
        <div className="row">
          <div className="col-sm-6">
            {port}
          </div>
          <div className="col-sm-6">
            {!vr.ctx.value.isNew && <SearchValueLine ctx={ctxBasic} findOptions={{
              queryName: InitiationRequestEntity,
              filterOptions: [{ token: InitiationRequestEntity.token(a => a.fromDomain), value: vr.ctx.value }]
            }} extraButtons={c => <OperationButton size={"xs" as "sm"} className="ms-3" eoc={EntityOperationContext.fromTypeContext(vr.ctx, InitiationRequestOperation.CreateInitiationRequestFromProgram)}>
              {EntityOperations.withIcon(EntityControlMessage.Create.niceToString(), "code-merge")}
            </OperationButton>} />
            }
          </div>
        </div>]);

      vr.replaceLine(p => p.defaultDashboard, d => [
        <div className="row">
          <div className="col-sm-3">
            {d}
          </div>
          <div className="col-sm-9">
            {!vr.ctx.value.isNew && <DomainLabelsEditor ctx={vr.ctx.subCtx({ formGroupStyle: 'Basic' })} />}
          </div>
        </div>]);

      vr.addTab("appTabs", <Tab eventKey="risk" title={vr.ctx.subCtx(DomainRiskMixin).subCtx(p => p.riskManagement).niceName()} >
        <EntityDetail ctx={vr.ctx.subCtx(DomainRiskMixin).subCtx(p => p.riskManagement)} />
      </Tab>);

      vr.addTab("appTabs", <Tab eventKey="stakeholders" title={StakeholderEntity.nicePluralName()} >
        <EntityDetail ctx={vr.ctx.subCtx(DomainStakeholderMixin).subCtx(p => p.stakeholderStrategies)} />
      </Tab>);

      vr.addTab("appTabs", <Tab eventKey="protocol" title={ProgramEntity.mixinNicePropertyName(DomainProtocolMixin, p => p.emailTemplate)} >
        <ProjectEmailTemplate ctx={vr.ctx.subCtx({ labelColumns: 4 })} />
      </Tab>);

      if (!vr.ctx.value.isNew) {
        vr.addTab("appTabs", <Tab eventKey="init" title={InitiationRequestPropertyEntity.nicePluralName()} >
          {!vr.ctx.value.isNew && <SearchControl findOptions={{
            queryName: InitiationRequestPropertyEntity,
            filterOptions: [{ token: InitiationRequestPropertyEntity.token(r => r.parent), value: vr.ctx.value }]
          }} />}
        </Tab>);
      }
    });

    Operations.addSettings(new ConstructorOperationSettings(ProgramExpandedOperation.Create, {
      onConstruct: coc => {
        const viewAndExecute = (model: NewProgramModel): Promise<EntityPack<ProgramEntity> | undefined> => {
          return Navigator.view(model)
            .then(m => {
              if (!m)
                return undefined;

              return coc.defaultConstruct(m)
                .then(p => { reloadTypesInDomains(); return p; },
                  e => ErrorModal.showErrorModal(e).then(() => viewAndExecute(m)));
            });
        };

        return viewAndExecute(NewProgramModel.New({
          name: "",
          programPrefix: "PGM",
          riskManagement: RiskManagementEmbedded.New(),
        }));
      }
    }));
  }
}
