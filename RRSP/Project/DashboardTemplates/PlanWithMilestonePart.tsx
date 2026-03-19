import { liteKey } from "@framework/Signum.Entities";
import { CustomPartProps } from "../../../Framework/Extensions/Signum.Dashboard/DashboardClient";
import { IDomainEntity } from "../../../Meros/Meros.Project/Meros.Project";
import { PlanOverview } from "../../../Meros/Meros.PortfolioExt/PlanOverview/PlanOverviewPage";

export default function PlanWithMilestonePart(p: CustomPartProps<IDomainEntity>): React.JSX.Element | string {
  return <PlanOverview liteKey={liteKey(p.entity!)} progressFromMilestones />;
}
