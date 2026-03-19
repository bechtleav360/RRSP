import { CustomPartProps } from "../../../Framework/Extensions/Signum.Dashboard/DashboardClient";
import { SearchControl } from "../../../Framework/Signum/React/Search";
import { PortfolioEntity, PortfolioQuery } from "../../../Meros/Meros.Project/Portfolio/Meros.Project.Portfolio";

export default function ProjectMainPart(p: CustomPartProps<PortfolioEntity>): React.JSX.Element | string {

  return (
    <div>
      <SearchControl findOptions={{
        queryName: PortfolioQuery.Domains,
        filterOptions: [{ token: PortfolioEntity.token(a => a), value: p.entity }],
      }} />
    </div>
  );
}
