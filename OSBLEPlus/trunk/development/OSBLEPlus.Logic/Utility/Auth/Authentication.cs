using System;
using System.Runtime.Caching;
using System.Web;
using OSBLE.Interfaces;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DataAccess.Profiles;

namespace OSBLEPlus.Logic.Utility.Auth
{
    public class Authentication : IAuthentication
    {
        private readonly FileCache _cache;
        private const string UserNameKey = "UserName";
        public const string ProfileCookieKey = "osble_profile";

        public Authentication()
            : this(HttpContext.Current.Server.MapPath("~\\App_Data\\"))
        {            
        }

        public Authentication(string path)
        {
            //set up cache
            _cache = FileCacheHelper.GetGlobalCacheInstance(FileCacheHelper.GetCachePath(path));
            _cache.DefaultRegion = "AuthenticationService";

            //have our cache kill things after 2 days
            _cache.DefaultPolicy = new CacheItemPolicy { SlidingExpiration = new TimeSpan(2, 0, 0, 0, 0) };
        }

        /// <summary>
        /// Returns true if the supplied key is valid
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public bool IsValidKey(string authToken)
        {
            var id = -1;
            try
            {
                id = (int)_cache[authToken];
                UserDataAccess.LogUserTransaction(id, DateTime.Now);
            }
            catch (Exception)
            {
                // ignored
            }

            return id >= 0;
        }

        public string GetAuthenticationKey()
        {
            if (HttpContext.Current != null)
            {
                var httpCookie = HttpContext.Current.Request.Cookies[ProfileCookieKey];
                if (httpCookie != null)
                    return httpCookie.Values[UserNameKey];
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the active user
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public IUser GetActiveUser(string authToken)
        {
            try
            {
                var id = (int)_cache[authToken];
                return UserDataAccess.GetById(id);
            }
            catch (Exception)
            {
                // ignored
                return null;
            }
        }

        public int GetActiveUserId(string authToken)
        {
            var id = -1;
            try
            {
                id = (int)_cache[authToken];
            }
            catch (Exception)
            {
                // ignored
            }
            return id;
        }

        /// <summary>
        /// Logs the user into the system
        /// </summary>
        /// <param name="profile"></param>
        public string LogIn(IUser profile)
        {
            var cookie = new HttpCookie(ProfileCookieKey);

            //compute hash for this login attempt
            var hash = GetPasswordHash(profile.Email);

            //store profile in the authentication hash
            _cache[hash] = profile.UserId;

            //store the key to the hash inside a cookie for the user
            cookie.Values[UserNameKey] = hash;

            //set a really long expiration date for the cookie.  Note that the server's copy of the
            //hash key will expire much sooner than this.
            cookie.Expires = DateTime.UtcNow.AddDays(360);

            //and then store it in the next response
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Response.Cookies.Set(cookie);
            }

            return hash;
        }

        /// <summary>
        /// Logs the current user out of the system
        /// </summary>
        public void LogOut()
        {
            if (HttpContext.Current == null) return;

            var httpCookie = HttpContext.Current.Request.Cookies[ProfileCookieKey];
            if (httpCookie != null)
                httpCookie.Expires = DateTime.UtcNow.AddDays(-1d);
        }

        public static string GetPasswordHash(string text)
        {
            return UserProfile.GetPasswordHash(text);
        }
    }
}