import { GlobalMessage } from './RRSP.Globals';
import { Lite } from '@framework/Signum.Entities';
import { AuthClient } from '../../Framework/Extensions/Signum.Authorization/AuthClient';
import { UserEntity, RoleEntity, LoginAuthMessage } from '../../Framework/Extensions/Signum.Authorization/Signum.Authorization';
import { ajaxGet } from '@framework/Services';

export namespace RRSPPasswordValidation {
  export const SpecialCharacters = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
  export const MinimumPasswordLength = 12;
  export const AdminMinimumPasswordLength = 14;

  export async function validatePassword(password: string, user: UserEntity): Promise<AuthClient.PasswordValidationResult | null> {
    if (!password) {
      return null;
    }

    if (user.role) {
      try {
        const fullRole = await ajaxGet<RoleEntity>({ url: `~/api/entity/${user.role.EntityType}/${user.role.id}` });
        var isAdmin = fullRole.inheritsFrom.length == 0 && fullRole.mergeStrategy == 'Intersection';
        if (isAdmin && password.length < AdminMinimumPasswordLength)
          return { message: LoginAuthMessage.ThePasswordMustHaveAtLeast0Characters.niceToString(AdminMinimumPasswordLength), level: "error" };
      } catch {
        // If fetch fails (e.g., user not authenticated), just use standard validation
      }
    }

    if (password.length < MinimumPasswordLength) {
      return { message: LoginAuthMessage.ThePasswordMustHaveAtLeast0Characters.niceToString(MinimumPasswordLength), level: "error" };
    }

    const complexityResult = checkPasswordComplexity(password);

    if (!complexityResult.hasUppercase) {
      return { message: GlobalMessage.PasswordMustContainUppercase.niceToString(), level: "warning" };
    }

    if (!complexityResult.hasLowercase) {
      return { message: GlobalMessage.PasswordMustContainLowercase.niceToString(), level: "warning" };
    }

    if (!complexityResult.hasDigit) {
      return { message: GlobalMessage.PasswordMustContainDigit.niceToString(), level: "warning" };
    }

    if (!complexityResult.hasSpecialChar) {
      return { message: GlobalMessage.PasswordMustContainSpecialChar.niceToString(), level: "warning" };
    }

    return null;
  }

  export interface PasswordComplexityResult {
    hasUppercase: boolean;
    hasLowercase: boolean;
    hasDigit: boolean;
    hasSpecialChar: boolean;
    meetsComplexity: boolean;
  }

  export function checkPasswordComplexity(password: string): PasswordComplexityResult {
    const hasUppercase = /[A-Z]/.test(password);
    const hasLowercase = /[a-z]/.test(password);
    const hasDigit = /\d/.test(password);
    const hasSpecialChar = SpecialCharacters.split('').some(c => password.includes(c));

    return {
      hasUppercase,
      hasLowercase,
      hasDigit,
      hasSpecialChar,
      meetsComplexity: hasUppercase && hasLowercase && hasDigit && hasSpecialChar
    };
  }
}
