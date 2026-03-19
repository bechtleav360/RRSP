import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks'
import { NewBoardEmbedded, NewColumnEmbedded, NewProjectModel } from '../RRSP.Project'
import { AutoLine, EntityDetail, EntityStrip, EntityTable, EnumLine } from '../../../Framework/Signum/React/Lines';
import { LessonsLearnedEntity } from '../../../Meros/Meros.PortfolioExt/LessonsLearned/Meros.PortfolioExt.LessonsLearned';
import { JavascriptMessage } from '@framework/Signum.Entities';
import { RiskManagementEmbedded } from '../../../Meros/Meros.Risk/Meros.Risk';
import { TaskState } from '../../../Meros/Meros.Tasks/Meros.Tasks';

export default function NewProject(p: { ctx: TypeContext<NewProjectModel> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();
  const [removedBoard, setRemovedBoard] = React.useState<NewBoardEmbedded | undefined>(undefined);
  const [removedRisk, setRemovedRisk] = React.useState<RiskManagementEmbedded | undefined>(undefined);
  const ctx = p.ctx.subCtx({ formGroupStyle: 'Basic' });

  return (
    <div>
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx.subCtx(p => p.projectName)} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx.subCtx(p => p.projectManager)} />
        </div>
      </div>
      <div className="row">
        <div className="col-sm-6">
          <AutoLine ctx={ctx.subCtx(p => p.members)} />
        </div>
        <div className="col-sm-6">
          <AutoLine ctx={ctx.subCtx(p => p.projectPrefix)} />
        </div>
      </div>
      <EntityDetail ctx={ctx.subCtx(p => p.riskManagement)}
        onCreate={() => Promise.resolve(removedRisk)}
        onRemove={r => {
          setRemovedRisk(r);
          return Promise.resolve(true);
        }} />
      <EntityDetail ctx={ctx.subCtx(p => p.createNewBoard)}
        getComponent={(ectx: TypeContext<NewBoardEmbedded>) => <EntityTable ctx={ectx.subCtx(c => c.columns)} move={false} columns={[
          {
            property: c => c.name,
            headerHtmlAttributes: { style: { width: '50%' } }

          },
          {
            property: c => c.taskState,
            template: (ctx) => <EnumLine ctx={ctx.subCtx(c => c.taskState)} optionItems={TaskState.values().filter(state => state !== "ArchivedDone" && state !== "Rejected")} />,
            headerHtmlAttributes: { style: { width: '20%' } }

          },
          {
            property: c => c.archiveTasksOlderThan,
            headerHtmlAttributes: { style: { width: '30%' } }
          },
        ]} />}          
        onCreate={() => Promise.resolve(removedBoard)}
        onRemove={b => {
        setRemovedBoard(b);
        return Promise.resolve(true);
      }} />
      <EntityStrip ctx={ctx.subCtx(p => p.createTasksFromLessonsLearned)} />
    </div>
  );
}


