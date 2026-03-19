//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Framework/Signum/React/Reflection'
import * as Entities from '../../Framework/Signum/React/Signum.Entities'
import * as Operations from '../../Framework/Signum/React/Signum.Operations'
import * as Portfolio from '../../Meros/Meros.Project/Portfolio/Meros.Project.Portfolio'
import * as Authorization from '../../Framework/Extensions/Signum.Authorization/Signum.Authorization'
import * as Risk from '../../Meros/Meros.Risk/Meros.Risk'


export const NewPortfolioModel: Type<NewPortfolioModel> = new Type<NewPortfolioModel>("NewPortfolioModel");
export interface NewPortfolioModel extends Entities.ModelEntity {
  Type: "NewPortfolioModel";
  name: string;
  type: Portfolio.PortfolioType;
  responsible: Entities.Lite<Authorization.UserEntity>;
  portfolioPrefix: string;
  riskManagement: Risk.RiskManagementEmbedded | null;
}

export namespace PortfolioExpandedOperation {
  export const Create : Operations.ConstructSymbol_Simple<Portfolio.PortfolioEntity> = registerSymbol("Operation", "PortfolioExpandedOperation.Create");
}

