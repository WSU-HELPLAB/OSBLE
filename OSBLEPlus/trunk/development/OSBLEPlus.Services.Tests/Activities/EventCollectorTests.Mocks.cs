using System;
using OSBLE.Interfaces;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Tests.Activities
{
    public class AuthenticationMock : IAuthentication
    {
        private IUser _user;
        private string _authToken;

        public bool IsValidKey(string authToken)
        {
            return authToken == _authToken;
        }

        public string GetAuthenticationKey()
        {
            return _authToken;
        }

        public IUser GetActiveUser(string authToken)
        {
            return IsValidKey(authToken) ? _user : null;
        }

        public int GetActiveUserId(string authToken)
        {
            return IsValidKey(authToken) ? _user.UserId : -1;
        }

        public string LogIn(IUser profile)
        {
            _user = profile;
            _authToken = profile.Email;
            return _authToken;
        }

        public void LogOut()
        {
            _authToken = null;
            _user = null;
        }

        public string GetPasswordHash(string text)
        {
            return text + DateTime.UtcNow.ToLongTimeString();
        }
    }
}
