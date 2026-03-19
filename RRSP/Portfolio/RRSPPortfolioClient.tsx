import { EntitySettings, Navigator } from '@framework/Navigator'
import { Operations, ConstructorOperationSettings } from '@framework/Operations'
import { EntityPack } from '@framework/Signum.Entities'
import { NewPortfolioModel, PortfolioExpandedOperation } from './RRSP.Portfolio'
import { PortfolioEntity, PortfolioType } from '../../Meros/Meros.Project/Portfolio/Meros.Project.Portfolio'
import { RiskManagementEmbedded, DomainRiskMixin } from '../../Meros/Meros.Risk/Meros.Risk'
import { DashboardClient } from '@extensions/Signum.Dashboard/DashboardClient'
import { DomainTaskMixin } from '../../Meros/Meros.Tasks/Meros.Tasks'
import { InitiationRequestPropertyEntity } from '../../Meros/Meros.PortfolioExt/InitiationRequest/Meros.PortfolioExt.InitiationRequest'
import { DomainLabelsEditor } from '../../Meros/Meros.PortfolioExt/DomainLabel/Templates/DomainLabel'
import { AutoLine, EntityDetail } from '@framework/Lines'
import { SearchControl } from '@framework/Search'
import { Tab } from 'react-bootstrap'
import ErrorModal from '@framework/Modals/ErrorModal'
import { reloadTypesInDomains } from '@framework/Reflection'

export namespace RRSPPortfolioClient {

  export function start(): void {

    Navigator.addSettings(new EntitySettings(NewPortfolioModel, p => import('./Templates/NewPortfolio')));

    DashboardClient.Options.registerCustomPartRenderer(PortfolioEntity, "GoalPart", () => import("../Project/DashboardTemplates/GoalPart"), { withPanel: false });
    DashboardClient.Options.registerCustomPartRenderer(PortfolioEntity, "PlanWithMilestonePart", () => import("../Project/DashboardTemplates/PlanWithMilestonePart"), { withPanel: true });
    DashboardClient.Options.registerCustomPartRenderer(PortfolioEntity, "MilestonePart", () => import("../Project/DashboardTemplates/MilestonePart"), { withPanel: false });

    Navigator.getSettings(PortfolioEntity)!.overrideView(vr => {
      vr.addTab("appTabs", <Tab eventKey="risk" title={vr.ctx.subCtx(DomainRiskMixin).subCtx(p => p.riskManagement).niceName()} >
        <EntityDetail ctx={vr.ctx.subCtx(DomainRiskMixin).subCtx(p => p.riskManagement)} avoidFieldSet />
      </Tab>);

      vr.addTab("appTabs", <Tab eventKey="Init" title={InitiationRequestPropertyEntity.nicePluralName()} >
        {!vr.ctx.value.isNew && <SearchControl findOptions={{
          queryName: InitiationRequestPropertyEntity,
          filterOptions: [{ token: InitiationRequestPropertyEntity.token(r => r.parent), value: vr.ctx.value }]
        }} />}
      </Tab>);

      vr.replaceLine(p => p.defaultDashboard, d => [
        <div className="row">
          <div className="col-sm-3">
            {d}
          </div>
          <div className="col-sm-9">
            {!vr.ctx.value.isNew && <DomainLabelsEditor ctx={vr.ctx.subCtx({ formGroupStyle: 'Basic' })} />}
          </div>
        </div>]);

      vr.replaceLine(p => p.purpose, man => [
        <div className="row">
          <div className="col-sm-9">
            {man}
          </div>
          <div className="col-sm-3">
            <AutoLine ctx={vr.ctx.subCtx(DomainTaskMixin).subCtx(p => p.prefix, { formGroupStyle: 'Basic' })} />
          </div>
        </div>]);
    });

    Operations.addSettings(new ConstructorOperationSettings(PortfolioExpandedOperation.Create, {
      onConstruct: coc => {
        const viewAndExecute = (model: NewPortfolioModel): Promise<EntityPack<PortfolioEntity> | undefined> => {
          return Navigator.view(model)
            .then(m => {
              if (!m)
                return undefined;

              return coc.defaultConstruct(m)
                .then(p => { reloadTypesInDomains(); return p; },
                  e => ErrorModal.showErrorModal(e).then(() => viewAndExecute(m)));
            });
        };

        return viewAndExecute(NewPortfolioModel.New({
          name: "",
          type: PortfolioType.value("Portfolio"),
          portfolioPrefix: "POR",
          riskManagement: RiskManagementEmbedded.New(),
        }));
      }
    }));
  }
}
