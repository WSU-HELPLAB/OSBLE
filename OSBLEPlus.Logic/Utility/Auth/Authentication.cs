using System;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Ionic.Zip;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DataAccess.Profiles;

namespace OSBLEPlus.Logic.Utility.Auth
{
    public class Authentication : IAuthentication
    {
        private readonly FileCache _cache;
        private const string UserNameKey = "UserName";
        public const string ProfileCookieKey = "osble_profile";
        public const string FileCacheKey = "FileCacheKey";
        public const string AuthKey = "AuthKey";

        public Authentication()
            : this(HttpContext.Current.Server.MapPath("~\\"))
        {
        }

        public Authentication(string path)
        {
            //set up cache
            _cache = FileCacheHelper.GetGlobalCacheInstance();            
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
                //var httpCookie = HttpContext.Current.Request.Cookies[FileCacheKey];

                //if (httpCookie != null)
                //{
                //    // need to decrype the username then find the key in the file system
                //    return httpCookie.Values[FileCacheKey];
                //}

                var httpCookie = HttpContext.Current.Request.Cookies[AuthKey];

                if (httpCookie != null)
                {
                    return httpCookie.Values[AuthKey];
                }

            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the active user
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public UserProfile GetActiveUser(string authToken)
        {
            if (null == authToken)
                return null;

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
        public string LogIn(UserProfile profile)
        {
            var cookie = new HttpCookie(AuthKey);

            //compute hash for this login attempt
            var hash = GetAuthKey(profile.Email);

            //store profile in the authentication hash
            _cache[hash] = profile.IUserId;

            //store the key to the hash inside a cookie for the user
            cookie.Values[AuthKey] = hash;

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

            // force delete cookies 
            try
            {
                foreach (string cookie in HttpContext.Current.Request.Cookies.AllKeys)
                {                    
                    HttpContext.Current.Response.Cookies[cookie].Expires = DateTime.Now.AddDays(-10);                    
                }                
            }
            catch (Exception ex)
            {
                // do nothing for now
                string foo = ex.Message;
            }

            if (HttpContext.Current.Session["auth"] != null)
                HttpContext.Current.Session["auth"] = null;
        }

        public static string GetOsblePasswordHash(string text)
        {
            return UserProfile.GetPasswordHash(text);
        }

        public static string GetAuthKey(string text)
        {
            var date = DateTime.UtcNow.ToLongTimeString();
            var hashString = text + date;

            //compute the hash
            using (var sha1 = new SHA1Managed())
            {
                var textBytes = Encoding.ASCII.GetBytes(hashString);
                var hashBytes = sha1.ComputeHash(textBytes);
                var hashText = BitConverter.ToString(hashBytes);

                //return the hash to the caller
                return hashText;
            }
        }
    }
}