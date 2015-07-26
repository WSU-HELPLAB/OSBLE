using OSBLE.Models.Users;

namespace OSBLEPlus.Logic.Utility.Auth
{
    public interface IAuthentication
    {
        bool IsValidKey(string authToken);
        string GetAuthenticationKey();
        UserProfile GetActiveUser(string authToken);
        int GetActiveUserId(string authToken);
        string LogIn(UserProfile profile);
        void LogOut();
    }
}
