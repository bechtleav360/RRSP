import { JavascriptMessage } from "@framework/Signum.Entities";
import { CustomPartProps } from "../../../Framework/Extensions/Signum.Dashboard/DashboardClient";
import { Finder } from "../../../Framework/Signum/React/Finder";
import { SearchControl } from "../../../Framework/Signum/React/Search";
import { GoalEntity } from "../../../Meros/Meros.PlanningProject/PSC/Meros.PlanningProject.PSC";
import { IDomainEntity } from "../../../Meros/Meros.Project/Meros.Project";
import { useAPI } from "../../../Framework/Signum/React/Hooks";
import { DashboardTemplateMessage } from "../RRSP.Project";
import { ProjectClient } from "../../../Meros/Meros.Project/ProjectClient";

export default function GoalPart(p: CustomPartProps<IDomainEntity>): React.JSX.Element | string {

  const goals = Finder.useFetchEntities({
    queryName: GoalEntity,
    filterOptions: [
      { token: GoalEntity.token(g => g.domain), value: p.entity },
      { token: GoalEntity.token(g => g.entity.parent), value: null },
    ]
  });

  const projects = useAPI(() => goals && Finder.getResultTableTyped({
    queryName: GoalEntity,
    filterOptions: [
      { token: GoalEntity.token(g => g.entity.appliesTo?.entity?.domain), value: p.entity },
    ],
    groupResults: true
  },
    {
      parent: GoalEntity.token(g => g.entity.appliesTo),
      domain: GoalEntity.token(g => g.domain),
    }).then(list => list.groupToMap(g => g.parent!.id?.toString(), g => g.domain)), [goals]);

  if (goals === undefined || projects === undefined)
    return JavascriptMessage.loading.niceToString();
  var selectedGoals = goals.orderBy(a => a.routeId).slice(0, 8);
  return (
    <div className="row">
      {selectedGoals.map(g =>
        <div className="col-3" key={g.id} title={g.description ? ProjectClient.stripHtml(g.description) : g.title}>
          <div className="card border-tertiary shadow-sm mb-3 o-hidden">
            <div className="card-body">
              <div className="text-center"><strong>{g.title.etc(50)}</strong></div>
              <h1 className="text-center">{projects.get(g.id?.toString())?.distinctBy(g => g).length ?? 0}</h1>
              <div className="text-center">{DashboardTemplateMessage.NumberOfProjects.niceToString()}</div>
              <div className="mt-3">
                <div className="progress">
                  <div className="progress-bar bg-success" style={{ width: `${g.progress * 100}%` }}></div>
                </div>
                <div className="text-center">{Math.round(g.progress * 100)} % {DashboardTemplateMessage.Progress.niceToString()}</div>
              </div>
            </div>
          </div>
        </div>)}
    </div>
  );
}
