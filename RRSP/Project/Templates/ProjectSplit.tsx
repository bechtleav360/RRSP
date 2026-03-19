import * as React from 'react'
import { TypeContext, AutoLine, EntityLine, EntityTable} from '@framework/Lines'
import { FindOptions } from '@framework/Search'
import { Navigator } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { useForceUpdate } from '@framework/Hooks'
import { Lite, Entity, is, newMListElement } from '@framework/Signum.Entities'
import { Type, getQueryKey } from '@framework/Reflection'
import { classes } from '@framework/Globals'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { DependencyList } from 'react'
import { ColumnEntity, TaskLabelEntity, TaskEntity, TaskMessage } from '../../../Meros/Meros.Tasks/Meros.Tasks'
import { ProjectEntity } from '../../../Meros/Meros.Project/Meros.Project'
import { ColumnActionEmbedded, LabelActionEmbedded, ProjectSplitModel, TaskActionEmbedded } from '../RRSP.Project'


export default function ProjectSplit(p: { ctx: TypeContext<ProjectSplitModel> }): React.JSX.Element {

  const forceUpdate = useForceUpdate();

  var [refreshKey, setRefreshKey] = React.useState(0);

  const ctx = p.ctx;
  const model = ctx.value;

  const selectedTasks = ctx.value.tasks.map(mle => mle.element.task);
  const selectedLabels = ctx.value.labels.map(mle => mle.element.label);
  const selectedColumns = ctx.value.columns.map(mle => mle.element.column);

  const handleAddTasks = (newTasks: Lite<TaskEntity>[]) => {
    ctx.value.tasks.push(...newTasks.map(t => newMListElement(TaskActionEmbedded.New({ task: t }))));
    ctx.value.modified = true;
    forceUpdate();
  }

  const handleAddLabels = (newLabels: Lite<TaskLabelEntity>[]) => {
    ctx.value.labels.push(...newLabels.map(l => newMListElement(LabelActionEmbedded.New({ label: l }))));
    ctx.value.modified = true;
    forceUpdate();
  }

  const handleAddColumns = (newColumns: Lite<ColumnEntity>[]) => {
    ctx.value.columns.push(...newColumns.map(c => newMListElement(ColumnActionEmbedded.New({ column: c }))));
    ctx.value.modified = true;
    forceUpdate();
  }


  return (
    <div>
      <button onClick={() => setRefreshKey(c => c+1)} className={"btn btn-xs btn-info"} color="0000aa" id="ExportPDF">
        Aktualisieren
      </button>

      <EntityLine ctx={ctx.subCtx(p => p.sourceProject)} findOptions={{
        queryName: ProjectEntity, filterOptions: [{ token: ProjectEntity.token(a => a.entity), operation: "DistinctTo", value: model.sourceProject }]
      }} readOnly
      />

      <EntityLine ctx={ctx.subCtx(p => p.targetProject)} findOptions={{
        queryName: ProjectEntity, filterOptions: [{ token: ProjectEntity.token(a => a.entity), operation: "DistinctTo", value: model.sourceProject }]
        }} create={Navigator.isCreable(ProjectEntity, { isSearch: true })} onChange={forceUpdate}
      />

      <EntityTable ctx={ctx.subCtx(p => p.tasks)} createOnBlurLastRow={false} onChange={forceUpdate}
        columns={[
          {
            property: p => p.task,
            template: (ectx) =>
              <EntityLine ctx={ectx.subCtx(p => p.task)} onChange={forceUpdate} findOptions={{ queryName: TaskEntity, filterOptions: [{ token: TaskEntity.token(a => a.entity.domain), value: model.sourceProject }] }} />,
            headerHtmlAttributes: { style: { width: "30%" } },
          },
          {
            header: TaskMessage.ChildTasks.niceToString(),
            template: ectx => ectx.value.task && <DoubleCounter findOptions={{
              queryName: TaskEntity,
              filterOptions: [
                { token: TaskEntity.token(a => a.entity.domain), value: model.sourceProject },
                { token: TaskEntity.token(a => a.entity).expression<TaskEntity>("ParentTask"), value: ectx.value.task }
              ]
            }} mandatory selected={selectedTasks} onAdd={handleAddTasks} deps={[refreshKey]} />
           
          },
          {
            header: TaskLabelEntity.nicePluralName(),
            template: ectx => ectx.value.task &&
              <DoubleCounter findOptions={{
                queryName: TaskLabelEntity,
                filterOptions: [
                  { token: TaskLabelEntity.token(a => a.entity).expression<TaskEntity[]>("Tasks").any(), value: ectx.value.task }
                ]
            }} mandatory selected={selectedLabels} onAdd={handleAddLabels} deps={[refreshKey]}/>
          },
          {
            header: ColumnEntity.nicePluralName(),
            template: ectx => ectx.value.task &&
              <DoubleCounter findOptions={{
                queryName: ColumnEntity,
                filterOptions: [
                  { token: ColumnEntity.token(a => a.entity).expression<TaskEntity[]>("Tasks").any(), value: ectx.value.task }
                ]
              }} mandatory selected={selectedColumns} onAdd={handleAddColumns} deps={[refreshKey]} />
          },
        ]}
      />


      <EntityTable ctx={ctx.subCtx(p => p.labels)} onChange={forceUpdate}
        columns={[
          {
            property: p => p.label,
            template: ectx => <EntityLine ctx={ectx.subCtx(p => p.label)} onChange={forceUpdate} findOptions={{
              queryName: TaskLabelEntity,
              filterOptions: [
                { token: TaskLabelEntity.token(a => a.entity.domain), value: model.sourceProject }
              ]
            }} />,
            headerHtmlAttributes: { style: { width: "30%" } },
          },
          {
            property: p => p.action,
            template: ectx => ectx.value.label &&
              <AutoLine ctx={ectx.subCtx(p => p.action)} onChange={forceUpdate}/>,
            headerHtmlAttributes: { style: { width: "20%" } },
          },
          ctx.value.labels.some(l => l.element.action == "Replace") ? {
            property: p => p.targetLabel,
            template: (ectx) => ectx.value.action == "Replace" &&
              <EntityLine ctx={ectx.subCtx(p => p.targetLabel)}
                findOptions={{
                  queryName: TaskLabelEntity,
                  filterOptions: [{ token: TaskLabelEntity.token(a => a.entity.domain), value: model.targetProject }]
                }} />,
            headerHtmlAttributes: { style: { width: "30%" } },
          } : undefined,
          {
            header: TaskEntity.nicePluralName(),
            template: (ectx, row) => ectx.value.label &&
              <DoubleCounter findOptions={{
                queryName: TaskEntity,
                filterOptions: [
                  { token: TaskEntity.token(t => t.entity.labels).any(), value: ectx.value.label }
              ]
              }} mandatory={ectx.value.action == "Move"} selected={selectedTasks} onAdd={handleAddTasks} deps={[refreshKey]} onChange={(already, entities) => {
                if (already.length == entities.length && (ectx.value.action == "Copy" || ectx.value.action == null)) {
                  ectx.value.action = "Move";
                  row.forceUpdate();
                } else if (already.length < entities.length && (ectx.value.action == "Move" || ectx.value.action == null)) {
                  ectx.value.action = "Copy";
                  row.forceUpdate();
                }
              }} />,
          },        
        ]}
      />

      <EntityTable ctx={ctx.subCtx(p => p.columns)} onChange={forceUpdate}
        columns={[
          {
            property: p => p.column,
            template: ctx => 
              <EntityLine ctx={ctx.subCtx(p => p.column)} onChange={forceUpdate}/>,
            headerHtmlAttributes: { style: { width: "30%" } },
          },
          {
            property: p => p.action,
            template: ctx => ctx.value.column &&
              <AutoLine ctx={ctx.subCtx(p => p.action)} onChange={forceUpdate}/>,
            headerHtmlAttributes: { style: { width: "20%" } },
          },
          ctx.value.columns.some(l => l.element.action == "Replace") ? {
            property: p => p.targetColumn,
            template: (ectx) => ectx.value.action == "Replace" &&
              <EntityLine ctx={ectx.subCtx(p => p.targetColumn)}
                findOptions={{
                  queryName: ColumnEntity, filterOptions: [
                    { token: ColumnEntity.token(a => a.entity.board.entity!.domain), value: model.targetProject },
                  ]
                }} />,
            headerHtmlAttributes: { style: { width: "30%" } },
          } : undefined,
          {
            header: TaskEntity.nicePluralName(),
            template: (ectx, row) => ectx.value.column && <DoubleCounter findOptions={{
              queryName: TaskEntity,
              filterOptions: [{ token: TaskEntity.token(t => t.entity.column), value: ectx.value.column }]
            }} mandatory={ectx.value.action == "Move"} selected={selectedTasks} onAdd={handleAddTasks} deps={[refreshKey]} onChange={(already, entities) => {
              if (already.length == entities.length && (ectx.value.action == "Copy" || ectx.value.action == null)) {
                ectx.value.action = "Move";
                row.forceUpdate();
              } else if (already.length < entities.length && (ectx.value.action == "Move" || ectx.value.action == null)) {
                ectx.value.action = "Copy";
                row.forceUpdate();
              }
            }} />
          },
        ]}
      />
     </div>
  );
}

export function DoubleCounter<T extends Entity>(p: {
  findOptions: FindOptions,
  selected: Lite<T>[],
  onAdd: (newEntites: Lite<T>[]) => void,
  mandatory?: boolean,
  deps?: DependencyList,
  onChange?: (already: Lite<T>[], entities: Lite<T>[]) => void
}): React.JSX.Element {

  const fo = p.findOptions;

  const type = fo.queryName as Type<T>;

  var entities = Finder.useFetchLites({
    queryName: type,
    filterOptions: fo.filterOptions,
    orderOptions: fo.orderOptions,
  }, p.deps);

  var already = entities?.filter(e => p.selected.some(s => is(s, e)));

  React.useEffect(() => {
    if (entities && already && p.onChange)
      p.onChange(already, entities);
  }, [already && entities && already.length == entities.length]);

  if (entities == null || already == null)
    return <span className="count-search badge badge-pill bg-secondary">…</span>;

  const color = entities.length == 0 ? "bg-secondary" :
    already.length == entities.length ? "bg-success" :
      p.mandatory ? "bg-danger" : "bg-warning";

  return (
    <div style={{ display: "flex", alignItems: "center" }} className="double-counter" data-query={getQueryKey(p.findOptions.queryName)}>
      <a href="#" className={classes("badge badge-pill", color)} 
        onClick={e => { e.preventDefault(); Finder.explore(p.findOptions); }}
      >{already.length} / {entities.length}</a>
      {already.length < entities.length && <a href="#" className={classes("sf-line-button sf-create")}
        onClick={e => { e.preventDefault(); p.onAdd(entities!.filter(e => !p.selected.some(s => is(s, e)))); }}
        title={TaskMessage.Include0.niceToString(type.niceCount(entities.length - already.length))}>
        <FontAwesomeIcon icon="square-plus" />
      </a>}
    </div>
  );
}
