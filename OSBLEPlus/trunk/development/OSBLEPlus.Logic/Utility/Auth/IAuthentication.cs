using OSBLEPlus.Logic.DomainObjects.Interfaces;

namespace OSBLEPlus.Logic.Utility.Auth
{
    public interface IAuthentication
    {
        bool IsValidKey(string authToken);
        string GetAuthenticationKey();
        IUser GetActiveUser(string authToken);
        int GetActiveUserId(string authToken);
        string LogIn(IUser profile);
        void LogOut();
    }
}
