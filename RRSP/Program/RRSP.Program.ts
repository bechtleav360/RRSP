//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Framework/Signum/React/Reflection'
import * as Entities from '../../Framework/Signum/React/Signum.Entities'
import * as Operations from '../../Framework/Signum/React/Signum.Operations'
import * as Authorization from '../../Framework/Extensions/Signum.Authorization/Signum.Authorization'
import * as Risk from '../../Meros/Meros.Risk/Meros.Risk'
import * as Program from '../../Meros/Meros.Project/Program/Meros.Project.Program'


export const NewProgramModel: Type<NewProgramModel> = new Type<NewProgramModel>("NewProgramModel");
export interface NewProgramModel extends Entities.ModelEntity {
  Type: "NewProgramModel";
  name: string;
  manager: Entities.Lite<Authorization.UserEntity>;
  programPrefix: string;
  riskManagement: Risk.RiskManagementEmbedded | null;
}

export namespace ProgramExpandedOperation {
  export const Create : Operations.ConstructSymbol_Simple<Program.ProgramEntity> = registerSymbol("Operation", "ProgramExpandedOperation.Create");
}

