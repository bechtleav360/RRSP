import * as React from 'react'
import { AutoLine, EntityLine, TypeContext, RenderEntity, TextAreaLine, EntityDetail } from '@framework/Lines'
import { Tabs, Tab } from 'react-bootstrap';
import { FileLine } from '@extensions/Signum.Files/Files';
import { ApplicationConfigurationEntity } from '../RRSP.Globals';
import { DashboardEntity } from '../../../Framework/Extensions/Signum.Dashboard/Signum.Dashboard';
import { DomainLevel, DomainRoleEntity, ProjectEntity } from '../../../Meros/Meros.Project/Meros.Project';
import { PortfolioEntity } from '../../../Meros/Meros.Project/Portfolio/Meros.Project.Portfolio';
import { ProgramEntity } from '../../../Meros/Meros.Project/Program/Meros.Project.Program';

export default function ApplicationConfiguration(p: { ctx: TypeContext<ApplicationConfigurationEntity> }): React.JSX.Element {
  const ctx = p.ctx;
  const ctx6 = p.ctx.subCtx({ labelColumns: 6 });
  return (
    <div>
      <AutoLine ctx={ctx.subCtx(a => a.environment)} />
      <Tabs id="appTabs">
        <Tab eventKey="email" title={ctx.niceName(a => a.email)}>
          <RenderEntity ctx={ctx.subCtx(a => a.email)} />
          <AutoLine ctx={ctx.subCtx(a => a.emailSender)} />
          <AutoLine ctx={ctx.subCtx(a => a.defaultAccountingReceiver)} />
        </Tab>
        <Tab eventKey="auth" title={ctx.niceName(a => a.authTokens)}>
          <RenderEntity ctx={ctx.subCtx(a => a.authTokens)} />
        </Tab>
        <Tab eventKey="folder" title={ctx.niceName(a => a.folders)}>
          <RenderEntity ctx={ctx.subCtx(a => a.folders)} />
        </Tab>
        <Tab eventKey="translation" title={ctx.niceName(a => a.translation)}>
          <RenderEntity ctx={ctx.subCtx(a => a.translation)} />
        </Tab>
        <Tab eventKey="task" title={ctx.niceName(a => a.task)}>
          <RenderEntity ctx={ctx.subCtx(a => a.task)} />
        </Tab>
        <Tab eventKey="proMT" title="ProMT">
          <div className="row">
            <div className="col-sm-6">
              <EntityLine ctx={ctx6.subCtx(a => a.defaultProjectManagerRole)} createOnFind={false} findOptions={{
                queryName: DomainRoleEntity,
                filterOptions: [{ token: DomainRoleEntity.token(a => a.entity).expression("HasProjectWriteAccess"), value: true, frozen: true }]
              }} />
              <AutoLine ctx={ctx6.subCtx(a => a.defaultProjectMemberRole)} />
              <EntityLine ctx={ctx6.subCtx(a => a.defaultProgramMangerRole)} createOnFind={false} findOptions={{
                queryName: DomainRoleEntity,
                filterOptions: [{ token: DomainRoleEntity.token(a => a.entity).expression("HasProgramWriteAccess"), value: true, frozen: true }]
              }} />
              <EntityLine ctx={ctx6.subCtx(a => a.defaultPortfolioMangerRole)} createOnFind={false} findOptions={{
                queryName: DomainRoleEntity,
                filterOptions: [{ token: DomainRoleEntity.token(a => a.entity).expression("HasPortfolioWriteAccess"), value: true, frozen: true }]
              }} />
              <AutoLine ctx={ctx6.subCtx(a => a.meetingProtocolReportTemplate)} />
              <AutoLine ctx={ctx6.subCtx(a => a.riskManagementPercentReportTemplate)} />
              <AutoLine ctx={ctx6.subCtx(a => a.changeRequestReportTemplate)} />
              <AutoLine ctx={ctx6.subCtx(a => a.workPackageReportTemplate)} />
              <FileLine ctx={ctx6.subCtx(a => a.meetingProtocolExcelTemplate)} />
            </div>
            <div className="col-sm-6">
              <AutoLine ctx={ctx6.subCtx(a => a.riskManagementReportTemplate)} />
              <AutoLine ctx={ctx6.subCtx(a => a.contactPersonReportTemplate)} />
              <AutoLine ctx={ctx6.subCtx(a => a.statusReportReportTemplate)} />
              <AutoLine ctx={ctx6.subCtx(a => a.businessCaseReportTemplate)} />
              <AutoLine ctx={ctx6.subCtx(a => a.projectCharterReportTemplate)} />
              <AutoLine ctx={ctx6.subCtx(a => a.perDayPrice)} />
            </div>
          </div>
          <EntityDetail ctx={ctx.subCtx(a => a.defaultDashboard)} getComponent={dctx => <div>
            <div className="row">
              <div className="col-sm-4">
                <EntityLine ctx={dctx.subCtx(a => a.portfolio)} labelColumns={3} findOptions={{
                  queryName: DashboardEntity,
                  filterOptions: [{ token: DashboardEntity.token(d => d.entityType!.entity!.cleanName), value: PortfolioEntity.typeName }]
                }} />
              </div>
              <div className="col-sm-4">
                <EntityLine ctx={dctx.subCtx(a => a.program)} labelColumns={3} findOptions={{
                  queryName: DashboardEntity,
                  filterOptions: [{ token: DashboardEntity.token(d => d.entityType!.entity!.cleanName), value: ProgramEntity.typeName }]
                }} />
              </div>
              <div className="col-sm-4">
                <EntityLine ctx={dctx.subCtx(a => a.project)} labelColumns={3} findOptions={{
                  queryName: DashboardEntity,
                  filterOptions: [{ token: DashboardEntity.token(d => d.entityType!.entity!.cleanName), value: ProjectEntity.typeName }]
                }} />
              </div>
            </div>
          </div>} />
        </Tab>
        <Tab eventKey="signature" title={ApplicationConfigurationEntity.nicePropertyName(ac => ac.emailSignature)} style={{}}>
          <TextAreaLine ctx={ctx.subCtx(a => a.emailSignature, { formGroupStyle: 'None' })} autoResize
            valueHtmlAttributes={{ style: { minHeight: 150 } }} />
        </Tab>
      </Tabs>
    </div>
  );
}
