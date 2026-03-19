import * as React from 'react'
import { useForceUpdate, useVersion } from '@framework/Hooks'
import { SearchControl } from '@framework/Search';
import { Finder } from '@framework/Finder';
import { JavascriptMessage, Lite, getMixin, getToString, newMListElement, parseLite } from '@framework/Signum.Entities';
import { StakeholderEntity } from '../../../Meros/Meros.Stakeholder/Meros.Stakeholder';
import { TaskEntity, TaskState } from '../../../Meros/Meros.Tasks/Meros.Tasks';
import { TaskStakeholderMixin } from '../RRSP.Project';
import { Constructor } from '../../../Framework/Signum/React/Constructor';
import { Navigator } from '../../../Framework/Signum/React/Navigator';
import { useParams } from 'react-router'
import { IDomainEntity } from '../../../Meros/Meros.Project/Meros.Project';
import { Accordion } from 'react-bootstrap'
import { GlobalMessage } from '../../Globals/RRSP.Globals';
import { typeAllowedInDomain } from '@framework/Reflection';

export default function CommunicationManagementPage(): React.JSX.Element | string {
  const params = useParams() as { domainKey: string };
  const domain = React.useMemo(() => parseLite(params.domainKey) as Lite<IDomainEntity>, []);

  const [version, updateVersion] = useVersion();

  const stakeholders = Finder.useFetchLites<StakeholderEntity>({
    queryName: StakeholderEntity,
    filterOptions: [
      { token: StakeholderEntity.token(s => s.domain), value: domain},
      { token: StakeholderEntity.token(s => s.entity).expression("HasOpenTask"), value: true }
    ],
    orderOptions: [{ token: StakeholderEntity.token(s => s.name), orderType: 'Ascending' }]
  }, [version]);

  Navigator.useEntityChanged(TaskEntity, () => updateVersion(), []);

  if (stakeholders === undefined)
    return JavascriptMessage.loading.niceToString();

  return (
    <div style={{ height: 'calc(100vh - 100px)', overflow: 'auto' }}>
      <h2>{GlobalMessage.CommunicationManagement.niceToString()}</h2>
      {stakeholders.length === 0 ? (
        <div className="mt-3 text-muted">
          <h5>{GlobalMessage.TasksAppearHereAsSoonAsTheyAreLinkedToAStakeholder.niceToString()}</h5>
        </div>
      ) : (
        <Accordion className='mt-3 px-5'>
          {stakeholders?.map(s =>
            <Accordion.Item key={s.id} className={"sf-accordion-element mb-3"} eventKey={s.id?.toString()!} >
              <Accordion.Header>
                <strong>{getToString(s)}</strong>
              </Accordion.Header>
              <Accordion.Body>
                <SearchControl
                  create={typeAllowedInDomain(TaskEntity, domain, true)}
                  onCreate={(sc) => Constructor.construct(TaskEntity, { domain: domain }).then(t => {
                    if (!t)
                      return Promise.resolve(undefined);
                    var mixin = getMixin(t, TaskStakeholderMixin);
                    mixin.stakeholders.push(newMListElement(s));
                    return Navigator.view(t);
                  })}
                  findOptions={{
                    queryName: TaskEntity,
                    filterOptions: [
                      { token: TaskEntity.token(t => t.entity).mixin(TaskStakeholderMixin).append(t => t.stakeholders).any(), value: s, frozen: true },
                      { token: TaskEntity.token(t => t.state), operation: 'IsIn', value: [TaskState.value('Open'), TaskState.value('InProgress')], frozen: true }
                    ],
                    orderOptions: [
                      { token: TaskEntity.token(t => t.title), orderType: 'Ascending' }
                    ]
                  }}
                />
              </Accordion.Body>
            </Accordion.Item>)}
        </Accordion>
      )}
    </div>
  );
}
