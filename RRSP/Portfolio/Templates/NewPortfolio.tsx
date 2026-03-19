import * as React from 'react'
import { TypeContext } from '@framework/TypeContext'
import { NewPortfolioModel } from '../RRSP.Portfolio'
import { AutoLine } from '@framework/Lines';

export default function NewPortfolio(p: { ctx: TypeContext<NewPortfolioModel> }): React.JSX.Element {
  const ctx = p.ctx.subCtx({ formGroupStyle: 'Basic' });

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(p => p.name)} />
      <div className="row">
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(p => p.type)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(p => p.responsible)} />
        </div>
        <div className="col-sm-4">
          <AutoLine ctx={ctx.subCtx(p => p.portfolioPrefix)} />
        </div>
      </div>
      <AutoLine ctx={ctx.subCtx(p => p.riskManagement)} />
    </div>
  );
}
