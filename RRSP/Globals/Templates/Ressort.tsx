import * as React from 'react'
import { TypeContext, AutoLine } from '@framework/Lines'
import { RessortEntity, UserMixin } from '../RRSP.Globals';
import { SearchControl } from '../../../Framework/Signum/React/Search';
import { UserEntity } from '../../../Framework/Extensions/Signum.Authorization/Signum.Authorization';

export default function Ressort(p: { ctx: TypeContext<RessortEntity> }): React.JSX.Element {
  var ctx = p.ctx;

  return (
    <div>
      <AutoLine ctx={ctx.subCtx(a => a.name)} />
      {ctx.value.isNew || <SearchControl findOptions={{
        queryName: UserEntity,
        filterOptions: [{ token: UserEntity.token(u => u.entity).mixin(UserMixin).append(u => u.ressort), value: ctx.value, frozen: true }]
      }}
      />}
    </div>
  );
}
