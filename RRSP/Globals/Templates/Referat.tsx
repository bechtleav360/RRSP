import * as React from 'react'
import { TypeContext, AutoLine } from '@framework/Lines'
import { ReferatEntity, UserMixin } from '../RRSP.Globals';
import { SearchControl } from '../../../Framework/Signum/React/Search';
import { UserEntity } from '../../../Framework/Extensions/Signum.Authorization/Signum.Authorization';

export default function Referat(p: { ctx: TypeContext<ReferatEntity> }): React.JSX.Element {
  var ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(a => a.name)} />
      {ctx.value.isNew || <SearchControl findOptions={{
        queryName: UserEntity,
        filterOptions: [{ token: UserEntity.token(u => u.entity).mixin(UserMixin).append(u => u.referat), value: ctx.value, frozen: true }]
      }}
      />}
    </div>
  );
}
