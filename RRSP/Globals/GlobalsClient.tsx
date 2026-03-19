import * as React from 'react'
import { RouteObject } from 'react-router'
import { Navigator, EntitySettings } from '@framework/Navigator'
import { Finder } from '@framework/Finder'
import { Operations, EntityOperationSettings, EntityOperationGroup } from '@framework/Operations'
import { Lite, getMixin, newMListElement } from '@framework/Signum.Entities'
import { RoleEntity, UserEntity, UserOperation } from '@extensions/Signum.Authorization/Signum.Authorization'
import { AutoLine, CheckboxLine } from '@framework/Lines'
import { ApplicationConfigurationEntity, UserExpandedOperation, RoleMixin, GlobalMessage, UserMixin, RessortEntity, ReferatEntity } from './RRSP.Globals'
import { SearchValueLine } from '@framework/Search';
import { RiskEntity } from '../../Meros/Meros.Risk/Meros.Risk'
import { MilestoneEmbedded, NextActivityEmbedded, PendingDecisionEmbedded, StatusReportMessage, RiskOrIssueEmbedded } from '../../Meros/Meros.StatusReport/Meros.StatusReport'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { MilestoneEntity, ProtocolPointEntity } from '../../Meros/Meros.Protocol/Meros.Protocol'
import { MemberEntity, ProjectOperation, UserProjectMixin,  } from '../../Meros/Meros.Project/Meros.Project'
import { AuthClient } from '../../Framework/Extensions/Signum.Authorization/AuthClient'
import { DashboardClient } from '../../Framework/Extensions/Signum.Dashboard/DashboardClient'
import { StatusReportClient } from '../../Meros/Meros.StatusReport/StatusReportClient'
import { LinkButton } from '../../Framework/Signum/React/Basics/LinkButton'
import { ProjectClient } from '../../Meros/Meros.Project/ProjectClient'
import { symbolNiceName } from '@framework/Reflection'

export namespace GlobalsClient {


  export function start(options: { routes: RouteObject[] }): void {

    DashboardClient.GlobalVariables.set('FirstName', () => getMixin(AuthClient.currentUser(), UserProjectMixin).firstName);
    DashboardClient.GlobalVariables.set('LastName', () => getMixin(AuthClient.currentUser(), UserProjectMixin).lastName);

    Navigator.addSettings(new EntitySettings(ApplicationConfigurationEntity, a => import('./Templates/ApplicationConfiguration')));
    Navigator.addSettings(new EntitySettings(RessortEntity, a => import('./Templates/Ressort')));
    Navigator.addSettings(new EntitySettings(ReferatEntity, a => import('./Templates/Referat')));

    Navigator.getSettings(UserEntity)!.overrideView((rep) => {

      rep.insertBeforeLine(u => u.state, ctx => [
        <div className="row">
          <div className="col-sm-4">
            <AutoLine ctx={ctx.subCtx(UserProjectMixin).subCtx(um => um.firstName)} formGroupStyle="Basic" />
          </div>
          <div className="col-sm-4">
            <AutoLine ctx={ctx.subCtx(UserProjectMixin).subCtx(um => um.lastName)} formGroupStyle="Basic" />
          </div>
          <div className="col-sm-4">
            <AutoLine ctx={ctx.subCtx(UserProjectMixin).subCtx(um => um.phoneNumber)} formGroupStyle="Basic" />
          </div>
        </div>,
        <div className="row">
          <div className="col-sm-4">
            <AutoLine ctx={ctx.subCtx(UserMixin).subCtx(um => um.ressort)} formGroupStyle="Basic" />
          </div>
          <div className="col-sm-4">
            <AutoLine ctx={ctx.subCtx(UserMixin).subCtx(um => um.referat)} formGroupStyle="Basic" />
          </div>
        </div>,
      ]);

      rep.insertAfterLine(u => u.cultureInfo, ctx => [
        <>
          <div className="row">
            <div className="col-sm-6">
              {!ctx.value.isNew && <SearchValueLine label={GlobalMessage.ProjectsProgramsPortfolios.niceToString()} ctx={ctx.subCtx({ labelColumns: 8 })} findOptions={{
                queryName: MemberEntity,
                filterOptions: [{ token: MemberEntity.token(a => a.user), value: ctx.value, frozen: true }]
              }} />}
            </div>
          </div>
        </>
      ]);
    });   

    Navigator.getSettings(RoleEntity)!.overrideView((rep) => {
      rep.insertAfterLine(r => r.mergeStrategy, ctx => [
        <AutoLine ctx={ctx.subCtx(RoleMixin).subCtx(r => r.type)} />,
        <CheckboxLine ctx={ctx.subCtx(RoleMixin).subCtx(r => r.administratedByBillingUser)} inlineCheckbox />,
      ]);
    });

    const deleteGroup: EntityOperationGroup = { key: "Delete", text: () => symbolNiceName(UserOperation.Delete), color: 'danger' };

    Operations.addSettings(new EntityOperationSettings(UserOperation.Delete, {
      group: deleteGroup
    }));

    Operations.addSettings(new EntityOperationSettings(UserExpandedOperation.DeleteWithAlternative, {
      group: deleteGroup,
      icon: "code-branch",
      onClick: eoc => Finder.find({
        queryName: UserEntity,
        filterOptions: [
          { token: UserEntity.token(a => a.entity), operation: "DistinctTo", value: eoc.entity },
          {
            groupOperation: "Or", filters: [
              { token: UserEntity.token(a => a.entity).mixin(UserProjectMixin).append(a => a.firstName), operation: "EqualTo", value: getMixin(eoc.entity, UserProjectMixin).firstName },
              { token: UserEntity.token(a => a.entity).mixin(UserProjectMixin).append(a => a.lastName), operation: "EqualTo", value: getMixin(eoc.entity, UserProjectMixin).lastName }
            ]
          }
        ],
        includeDefaultFilters: false,
      }, { message: GlobalMessage.PleaseSelectAnAlternative0.niceToString(UserEntity.nicePluralName()), searchControlProps: { showFilters: true } })
        .then(alt => alt && eoc.defaultClick(alt))
    }));
  }
}

