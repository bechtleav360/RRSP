using Signum.Authorization;

namespace RRSP.Globals;

public static class RRSPPasswordValidation
{
    public static string SpecialCharacters = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
    
    public static int MinimumPasswordLength = 12;
    public static int AdminMinimumPasswordLength = 14;

    public static string? ValidatePassword(string password, UserEntity? user)
    {
        if (string.IsNullOrEmpty(password))
        {
            return null;
        }

        if (password.Length < MinimumPasswordLength)
        {
            return LoginAuthMessage.ThePasswordMustHaveAtLeast0Characters.NiceToString(MinimumPasswordLength);
        }

        if (user?.Role != null)
        {
            var fullRole = user.Role.Retrieve();
            var isAdmin = fullRole.InheritsFrom.Count == 0 && fullRole.MergeStrategy == MergeStrategy.Intersection;
            
            if (isAdmin && password.Length < AdminMinimumPasswordLength)
            {
                return LoginAuthMessage.ThePasswordMustHaveAtLeast0Characters.NiceToString(AdminMinimumPasswordLength);
            }
        }

        return null;
    }
}
