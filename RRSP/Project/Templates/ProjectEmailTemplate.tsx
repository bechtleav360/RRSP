import * as React from 'react'
import { Finder } from '@framework/Finder'
import { TypeContext, EntityDetail, EntityLine } from '@framework/Lines'
import { newMListElement } from '@framework/Signum.Entities';
import { ProjectEntity } from '../../../Meros/Meros.Project/Meros.Project';
import { ProjectEmailTemplateEmbedded, DomainProtocolMixin } from '../../../Meros/Meros.Protocol/Meros.Protocol';
import { EmailTemplateEntity, EmailTemplateMessageEmbedded } from '@extensions/Signum.Mailing/Signum.Mailing.Templates';
import { EmailModelEntity } from '@extensions/Signum.Mailing/Signum.Mailing';
import { QueryEntity } from '@framework/Signum.Basics';
import { useAPI } from '@framework/Hooks';
import { MailingClient } from '@extensions/Signum.Mailing/MailingClient'
import SimpleEmailTemplate from './SimpleEmailTemplate';
import { DomainStatusReportMixin } from '../../../Meros/Meros.StatusReport/Meros.StatusReport';
import { Navigator } from '../../../Framework/Signum/React/Navigator';
import { ProgramEntity } from '../../../Meros/Meros.Project/Program/Meros.Project.Program';

export default function ProjectEmailTemplate(p: { ctx: TypeContext<ProjectEntity | ProgramEntity> }): React.JSX.Element {
  var ctx = p.ctx;
  var defaultCulture = useAPI(signal => MailingClient.API.getDefaultCulture(), []);
  var canCreate = Navigator.isCreable(EmailTemplateEntity);

  return (
    <EntityDetail ctx={ctx.subCtx(DomainProtocolMixin).subCtx(p => p.emailTemplate)} getComponent={(rctx: TypeContext<ProjectEmailTemplateEmbedded>) => {
      return (
        <div style={{ overflowX: 'hidden' }}>
          <EntityLine ctx={rctx.subCtx(a => a.meetingProtocolInternalEmailTemplate)}
            findOptions={{
              queryName: EmailTemplateEntity,
              filterOptions: [{ token: EmailTemplateEntity.token(et => et.entity.query!.key), value: "MeetingProtocol" }]
            }}

            create={canCreate}
            remove={canCreate}
            onCreate={async () => {
              var models = await Finder.fetchEntities({
                queryName: EmailModelEntity,
                filterOptions: [{ token: EmailModelEntity.token(em => em.fullClassName), value: "Meros.Protocol.MeetingProtocolEmailModel" }]
              });

              var queries = await Finder.fetchEntities({
                queryName: QueryEntity,
                filterOptions: [{ token: QueryEntity.token(q => q.key), value: "MeetingProtocol" }]
              });

              return Promise.resolve(EmailTemplateEntity.New({
                name: `Meeting Protocol Internal Email Template for ${ctx.value.name}`,
                model: models.first(),
                query: queries.first(),
                disableAuthorization: true,
                messageFormat: 'HtmlSimple',
                editableMessage: true,
                messages: [newMListElement(EmailTemplateMessageEmbedded.New({ cultureInfo: defaultCulture! }))]
              }))
            }}

            getComponent={(etctx: TypeContext<EmailTemplateEntity>) => <SimpleEmailTemplate ctx={etctx} />} />
          <EntityLine ctx={rctx.subCtx(a => a.meetingProtocolExternalEmailTemplate)}
            findOptions={{
              queryName: EmailTemplateEntity,
              filterOptions: [{ token: EmailTemplateEntity.token(et => et.entity.query!.key), value: "MeetingProtocol" }]
            }}

            create={canCreate}
            remove={canCreate}
            onCreate={async () => {
              var models = await Finder.fetchEntities({
                queryName: EmailModelEntity,
                filterOptions: [{ token: EmailModelEntity.token(em => em.fullClassName), value: "Meros.Protocol.MeetingProtocolEmailModel" }]
              });

              var queries = await Finder.fetchEntities({
                queryName: QueryEntity,
                filterOptions: [{ token: QueryEntity.token(q => q.key), value: "MeetingProtocol" }]
              });

              return Promise.resolve(EmailTemplateEntity.New({
                name: `Meeting Protocol External Email Template for ${ctx.value.name}`,
                model: models.first(),
                query: queries.first(),
                disableAuthorization: true,
                messageFormat: 'HtmlSimple',
                editableMessage: true,
                messages: [newMListElement(EmailTemplateMessageEmbedded.New({ cultureInfo: defaultCulture! }))]
              }))
            }}

            getComponent={(etctx: TypeContext<EmailTemplateEntity>) => <SimpleEmailTemplate ctx={etctx} />} />

          <EntityLine ctx={ctx.subCtx(DomainStatusReportMixin).subCtx(a => a.statusReportEmailTemplate)}
            findOptions={{
              queryName: EmailTemplateEntity,
              filterOptions: [{ token: EmailTemplateEntity.token(et => et.entity.query!.key), value: "StatusReport" }]
            }}
            create={canCreate}
            remove={canCreate}
            onCreate={async () => {
              var models = await Finder.fetchEntities({
                queryName: EmailModelEntity,
                filterOptions: [{ token: EmailModelEntity.token(em => em.fullClassName), value: "Meros.StatusReport.ProjectStatusReportEmailModel" }]
              });

              var queries = await Finder.fetchEntities({
                queryName: QueryEntity,
                filterOptions: [{ token: QueryEntity.token(q => q.key), value: "StatusReport" }]
              });

              return Promise.resolve(EmailTemplateEntity.New({
                name: `Status Report Email Template for ${ctx.value.name}`,
                model: models.first(),
                query: queries.first(),
                disableAuthorization: true,
                messageFormat: 'HtmlSimple',
                editableMessage: true,
                messages: [newMListElement(EmailTemplateMessageEmbedded.New({ cultureInfo: defaultCulture }))]
              }))
            }}
            getComponent={(etctx: TypeContext<EmailTemplateEntity>) => <SimpleEmailTemplate ctx={etctx} />} />
        </div>
      );
    }} />
  );
}
