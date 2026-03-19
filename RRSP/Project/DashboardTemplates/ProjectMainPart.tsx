import * as React from 'react'
import { Navigator } from '@framework/Navigator'
import { useAPI, useAPIWithReload, useForceUpdate } from '@framework/Hooks';
import { MemberEntity, ProjectEntity, ProjectPhase, UserProjectMixin } from '../../../Meros/Meros.Project/Meros.Project';
import { CustomPartProps } from '../../../Framework/Extensions/Signum.Dashboard/DashboardClient';
import "./ProjectMainPart.css"
import { getMixin, getToString, JavascriptMessage, liteKey } from '@framework/Signum.Entities';
import { DateTime } from 'luxon'
import { Finder } from '@framework/Finder';
import { typeAllowedInDomain } from '@framework/Reflection';
import { CustomerEntity } from '../../../Meros/Meros.Project/Customers/Meros.Project.Customers';
import { StatusReportEntity } from '../../../Meros/Meros.StatusReport/Meros.StatusReport';
import { OverallProgressCell } from '../../../Meros/Meros.StatusReport/StatusReportClient';
import { QueryTokenString, toNumberFormat } from '@framework/Reflection';
import { WizardHeader } from '../../../Meros/Meros.Project/Dashboard/WizardHeader';
import { BoardEntity } from '../../../Meros/Meros.Tasks/Meros.Tasks';
import { ProjectPlanMixin, ProjectWorkPackageEntity } from '../../../Meros/Meros.PlanningProject/WorkPackage/Meros.PlanningProject.WorkPackage';
import { RevenueEntity } from '../../../Meros/Meros.Financials/Meros.Financials';
import { FinancialsClient } from '../../../Meros/Meros.Financials/FinancialsClient';
import { DashboardTemplateMessage } from '../RRSP.Project';
import StackedProgressBar from '../../../Meros/Meros.Project/Templates/StackedProgressBar';
import { OrderProjectEntity } from '../../../Meros/Meros.Project/Orders/Meros.Project.Orders';
import { MemberResponsibilityEntity } from '../../../Meros/Meros.PortfolioExt/Responsibilities/Meros.PortfolioExt.Responsibilities';
import { DomainLabels } from '../../../Meros/Meros.PortfolioExt/DomainLabel/Templates/DomainLabel';

export default function ProjectMainPart(p: CustomPartProps<ProjectEntity>): React.JSX.Element | string {

  const proj = p.entity!;
  const forceUpdate = useForceUpdate();

  var projectEntity = useAPI(signal => Navigator.API.fetch(proj), [proj]);

  const members = useAPI(() => Finder.getQueryValue(MemberEntity, [
    {
      token: MemberEntity.token(a => a.domain), value: proj
    }
  ]), [proj]) as number | undefined;

  const [financialInfos] = useAPIWithReload(() =>
    FinancialsClient.API.getFinancialInfos(liteKey(proj)),
    [proj], { avoidReset: true });

  var boards = useAPI(() => proj == null ? Promise.resolve(null) : Finder.fetchLites({
    queryName: BoardEntity,
    filterOptions: [
      { token: BoardEntity.token(p => p.domain), value: proj },
    ],
  }), [proj]);

  const owners = useAPI(() => Finder.getResultTableTyped({
    queryName: MemberResponsibilityEntity,
    filterOptions: [
      { token: MemberResponsibilityEntity.token(a => a.member.entity!.domain), value: proj },
      { token: MemberResponsibilityEntity.token(a => a.responsibility.entity?.displayAsOwner), value: true},
    ]
  }, {
    lastName: MemberResponsibilityEntity.token(a => a.member.entity?.user).mixin(UserProjectMixin).append(u => u.lastName),
    firstName: MemberResponsibilityEntity.token(a => a.member.entity?.user).mixin(UserProjectMixin).append(u => u.firstName),
  }), [proj]);

  const psr = Finder.useResultTableTyped({
    queryName: ProjectEntity,
    filterOptions: [
      { token: ProjectEntity.token(a => a.entity), value: proj },
    ]
  }, {
    consumedTime: ProjectEntity.token(a => a.entity).expression<StatusReportEntity>("LastStatusReport").append(sr => sr.consumedTime),
    consumedCost: ProjectEntity.token(a => a.entity).expression<StatusReportEntity>("LastStatusReport").append(sr => sr.consumedCost),
    overalProgress: ProjectEntity.token(a => a.entity).expression<StatusReportEntity>("LastStatusReport").append(sr => sr.overallProgress),
    lastReportDate: ProjectEntity.token(a => a.entity).expression<StatusReportEntity>("LastStatusReport").append(sr => sr.reportDate)
  })?.firstOrNull();

  var projectPackage = Finder.useResultTableTyped(!projectEntity ? null : {
    queryName: ProjectWorkPackageEntity,
    groupResults: true,
    filterOptions: [{
      token: ProjectWorkPackageEntity.token(a => a.projectPlan),
      value: getMixin(projectEntity, ProjectPlanMixin).defaultPlan
    }],
  }, {
    status: ProjectWorkPackageEntity.token(a => a.entity.status),
    count: QueryTokenString.count
  });

  var phases = ProjectPhase.values();
  var currentIndex = phases.indexOf(projectEntity?.phase!);

  if (boards === undefined)
    return JavascriptMessage.loading.niceToString();

  function formatDate(a: string | null | undefined) {
    return a && DateTime.fromISO(a).toFormat("dd.MM.yyyy");
  }

  var formatter = toNumberFormat("C0");
  var formatterPercentage = toNumberFormat("P0");
  function formatEuro(value: number | undefined | null) {
    return value == null ? " - " : formatter.format(value) + " " + "€";
  }

  return (
    <div className="p-3 pt-0">
      <DomainLabels domain={proj} />
      <div className="phase-indicator d-flex align-items-center justify-content-center gap-2 mb-4 p-3 rounded" style={{ width: "100%" }}>
        <WizardHeader currentIndex={currentIndex} values={phases.map(p => ProjectPhase.niceToString(p))} clickable={false} />
      </div>
      <div className="row">
        <div className="col-lg-4">
          <div className="project-details mb-3">
            <span className="detail-label">{ProjectEntity.nicePropertyName(p => p.manager)}: </span>
            <span className="detail-value">{getToString(projectEntity?.manager)}</span>
            <span className="detail-label">{DashboardTemplateMessage.ProjectOwner.niceToString()}: </span>
            <span className="detail-value">{owners?.orderBy(o => o.lastName).map(o => `${o.lastName} ${o.firstName}`).join(", ")}</span>            
            <span className="detail-label">{DashboardTemplateMessage.Runtime.niceToString()}: </span>
            <span className="detail-value">{formatDate(projectEntity?.plannedStartDate!)} - {formatDate(projectEntity?.plannedEndDate!)}</span>
            <span className="detail-label">{MemberEntity.nicePluralName()}:</span>
            <span className="detail-value">{members}</span>
          </div>
        </div>
        {/* Conditional rendering for middle and right columns */}
        {RevenueEntity.tryTypeInfo() && typeAllowedInDomain(RevenueEntity, proj) ? (
          <>
            {/* User HAS financial access - progress in middle, financial on right */}
            <div className="col-lg-4 mt-4 mt-lg-0 d-flex flex-column">
              <div className="d-flex flex-column align-items-lg-end align-items-center gap-3">
                <div className="d-flex flex-column align-items-lg-end align-items-center gap-2 mb-3">
                  <h4 className="mb-0">{psr && psr.overalProgress && psr.overalProgress.progress != null && formatterPercentage.format(psr.overalProgress.progress)} </h4>
                  <div>
                    {psr && psr.overalProgress && psr.overalProgress.progress != null && <OverallProgressCell cell={psr.overalProgress} showPercentage={false} />}
                  </div>
                  {psr && psr.overalProgress ? <small className="text-muted"> {DashboardTemplateMessage.ProjectProgressWeighted.niceToString()} </small> : null}
                  <small className="text-muted">{projectEntity && (
                    !getMixin(projectEntity, ProjectPlanMixin).defaultPlan ? <span className="text-danger">{DashboardTemplateMessage.NoStandardPlan.niceToString()}</span> :
                      projectPackage == null ? "..." : <span>{DashboardTemplateMessage._0Of1WorkPackagesCompleted.niceToString(projectPackage.filter(a => a.status == "Done").sum(a => a.count), projectPackage.sum(a => a.count))}</span>
                  )}</small>
                </div>
              </div>
            </div>
            <div className="col-lg-4 mt-4 mt-lg-0 d-flex flex-column">
              <div className="d-flex flex-column align-items-lg-end align-items-center gap-3">
                {financialInfos && financialInfos.length > 0 && (() => {
                  const totalRevenue = financialInfos.flatMap(f => f.revenues).sum(r => r.amount);
                  const totalExpenditure = financialInfos.flatMap(f => f.expenditures).sum(e => e.amount);
                  const available = totalRevenue - totalExpenditure;
                  const utilization = totalRevenue > 0 ? totalExpenditure / totalRevenue : 0;
                  return (
                    <div className="d-flex flex-column align-items-lg-end align-items-center gap-2">
                      <h4 className="mb-0">{formatEuro(totalExpenditure)}</h4>
                      <small className="text-muted">{DashboardTemplateMessage.Of0TotalBudget.niceToString(formatEuro(totalRevenue))}</small>
                      <div style={{ width: "250px" }}>
                        <StackedProgressBar
                          maxValue={totalRevenue}
                          className="custom-bar"
                          values={[
                            {
                              forgroundValue: totalExpenditure,
                              backgroundValue: totalRevenue,
                              color: "success",
                            }
                          ]}
                        />
                      </div>
                      <small className="text-muted">{DashboardTemplateMessage.BudgetExhausted.niceToString()}: {formatterPercentage.format(utilization)}</small>
                      <small className="text-muted">{DashboardTemplateMessage.Available.niceToString()}: {formatEuro(available)}</small>
                    </div>
                  );
                })()}
              </div>
            </div>
          </>
        ) : (
          <>
            {/* User DOESN'T have financial access - progress moves to right */}
            <div className="col-lg-4 mt-4 mt-lg-0">
              {/* Empty middle column */}
            </div>
            <div className="col-lg-4 mt-4 mt-lg-0 d-flex flex-column">
              <div className="d-flex flex-column align-items-lg-end align-items-center gap-3">
                <div className="d-flex flex-column align-items-lg-end align-items-center gap-2 mb-3">
                  <h4 className="mb-0">{psr && psr.overalProgress && psr.overalProgress.progress != null && formatterPercentage.format(psr.overalProgress.progress)} </h4>
                  <div>
                    {psr && psr.overalProgress && psr.overalProgress.progress != null && <OverallProgressCell cell={psr.overalProgress} showPercentage={false} />}
                  </div>
                  {psr && psr.overalProgress ? <small className="text-muted"> {DashboardTemplateMessage.ProjectProgressWeighted.niceToString()} </small> : null}
                  <small className="text-muted">{projectEntity && (
                    !getMixin(projectEntity, ProjectPlanMixin).defaultPlan ? <span className="text-danger">{DashboardTemplateMessage.NoStandardPlan.niceToString()}</span> :
                      projectPackage == null ? "..." : <span>{DashboardTemplateMessage._0Of1WorkPackagesCompleted.niceToString(projectPackage.filter(a => a.status == "Done").sum(a => a.count), projectPackage.sum(a => a.count))}</span>
                  )}</small>
                </div>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
