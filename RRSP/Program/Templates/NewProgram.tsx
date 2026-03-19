import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { NewProgramModel } from '../RRSP.Program'
import { AutoLine } from '@framework/Lines';

export default function NewProgram(p: { ctx: TypeContext<NewProgramModel> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: 'Basic' });

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(p => p.name)} />
      <div className="row">
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(p => p.manager)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(p => p.programPrefix)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(p => p.riskManagement)} />
        </div>
      </div>
    </div>
  );
}
