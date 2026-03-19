import * as React from 'react'
import { AutoLine, EntityTabRepeater } from '@framework/Lines'
import { TypeContext } from '@framework/TypeContext'
import { useForceUpdate } from '@framework/Hooks'
import { EmailTemplateMessageComponent } from '@extensions/Signum.Mailing/Templates/EmailTemplate'
import { SearchValueLine } from '@framework/Search'
import { EmailTemplateEntity, EmailTemplateMessageEmbedded } from '@extensions/Signum.Mailing/Signum.Mailing.Templates'
import { ProjectEntity } from '../../../Meros/Meros.Project/Meros.Project'
import { DomainProtocolMixin } from '../../../Meros/Meros.Protocol/Meros.Protocol'
import { DomainStatusReportMixin } from '../../../Meros/Meros.StatusReport/Meros.StatusReport'

export default function SimpleEmailTemplate(p: { ctx: TypeContext<EmailTemplateEntity> }): React.JSX.Element {
  const forceUpdate = useForceUpdate();

  const ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(a => a.name)} />
      {!ctx.value.isNew && <SearchValueLine ctx={ctx} findOptions={{
        queryName: ProjectEntity,
        filterOptions: [
          ctx.value.model?.fullClassName == "Meros.Protocol.MeetingProtocolEmailModel" ?
            {
              groupOperation: 'Or',
              filters: [
                { token: ProjectEntity.token(a => a.entity).mixin(DomainProtocolMixin).append(p => p.emailTemplate?.meetingProtocolInternalEmailTemplate), value: ctx.value },
                { token: ProjectEntity.token(a => a.entity).mixin(DomainProtocolMixin).append(p => p.emailTemplate?.meetingProtocolExternalEmailTemplate), value: ctx.value },
              ]
            }
             :
            ctx.value.model?.fullClassName == "Meros.ProjectStatusReport.ProjectStatusReportEmailModel" ? 
              { token: ProjectEntity.token(a => a.entity).mixin(DomainStatusReportMixin).append(p => p.statusReportEmailTemplate), value: ctx.value } :
              undefined
        ]
      }} />}

      <div className="sf-email-replacements-container">
        <EntityTabRepeater ctx={ctx.subCtx(a => a.messages)} onChange={() => forceUpdate()} getComponent={(ec: TypeContext<EmailTemplateMessageEmbedded>) =>
          <EmailTemplateMessageComponent ctx={ec} queryKey={ctx.value.query!.key!} messageFormat={'HtmlSimple'} invalidate={() => forceUpdate()} />} />
      </div>
    </div>
  );
}


