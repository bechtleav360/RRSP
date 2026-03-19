import { JavascriptMessage, translated } from "@framework/Signum.Entities";
import { CustomPartProps, DashboardTooltipIcon } from "../../../Framework/Extensions/Signum.Dashboard/DashboardClient";
import { Finder } from "../../../Framework/Signum/React/Finder";
import { SearchControl } from "../../../Framework/Signum/React/Search";
import { GoalEntity } from "../../../Meros/Meros.PlanningProject/PSC/Meros.PlanningProject.PSC";
import { IDomainEntity, ProjectEntity } from "../../../Meros/Meros.Project/Meros.Project";
import { useAPI } from "../../../Framework/Signum/React/Hooks";
import { DashboardTemplateMessage } from "../RRSP.Project";
import { toNumberFormat } from "@framework/Reflection";
import ProgressBar from "../../../Framework/Signum/React/Components/ProgressBar";
import { MilestoneMessage } from "../../../Meros/Meros.Protocol/Meros.Protocol";

export default function MilestonePart(p: CustomPartProps<IDomainEntity>): React.JSX.Element | string {

  const info = useAPI(() => p.entity && Finder.inDBMany(p.entity, {
    count: ProjectEntity.token(p => p.entity).expression<string>("AllAccomplishedMilestones"),
    progress: ProjectEntity.token(p => p.entity).expression<number>("AllMilestonesProgress"),
  }), []);

  const tooltipHtml = translated(p.partEmbedded, pe => pe.tooltip);

  if (info === undefined)
    return JavascriptMessage.loading.niceToString();

  return (
    <div className="card border-tertiary shadow-sm mb-3 o-hidden">
      <div className="card-body mb-1">
        <div className="row align-items-center mb-2">
          <div className="col-sm-3">
            <h3 className='m-0'>{info.count}</h3>
          </div>
          <div className="col-sm-9">
            <ProgressBar containerHtmlAttributes={{ style: { height: 22 } }} value={info.progress} showPercentageInMessage color='success' />
          </div>
        </div>       
        <h6 className='mb-1'>
          {MilestoneMessage.AccomplishedMilestones.niceToString()}
          {tooltipHtml && (
            <DashboardTooltipIcon
              tooltipHtml={tooltipHtml}
              className="ms-2"
              iconClassName="sf-tooltip-icon"
            />
          )}
        </h6>
      </div>
    </div>
  );
}
