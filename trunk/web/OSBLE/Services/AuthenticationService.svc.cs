using System;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Collections.Generic;
using OSBLE.Models;
using OSBLE.Models.Services.Uploader;
using System.Web.Security;
using OSBLE.Models.Users;
using System.Security.Cryptography;
using OSBLE.Utility;
using System.Runtime.Caching;

namespace OSBLE.Services
{
    /// <summary>
    /// </summary>
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class AuthenticationService
    {
        private const double SESSION_TIMEOUT_IN_MINUTES = -15.0;
        private FileCache _cache;
        private OSBLEContext _db = new OSBLEContext();

        public AuthenticationService()
        {
            _cache = FileCacheHelper.GetGlobalCacheInstance();
            _cache.DefaultRegion = "AuthenticationService";

            //have our cache kill things after 30 minutes
            _cache.DefaultPolicy = new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, 0, 30, 0, 0) };
        }

        /// <summary>
        /// Returns true if the supplied key is valid
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public bool IsValidKey(string authToken)
        {
            UserProfile profile = _cache[authToken] as UserProfile;
            if (profile == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the active user
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public UserProfile GetActiveUser(string authToken)
        {
            UserProfile profile = _cache[authToken] as UserProfile;
            if (profile == null)
            {
                return new UserProfile();
            }
            return profile;
        }

        /// <summary>
        /// Validates the supplied user/pass combination.  This should be the first
        /// thing that you call when establishing a new connection.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>A unique key needed to make most web service calls.</returns>
        [OperationContract]
        public string ValidateUser(string userName, string password)
        {

            if (UserProfile.ValidateUser(userName, password))
            {
                UserProfile profile = (from p in _db.UserProfiles
                                       where p.AspNetUserName == userName
                                       select p).First();

                //build our string to hash
                string email = profile.UserName;
                string date = DateTime.Now.ToLongTimeString();
                string hashString = email + date;

                //compute the hash
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                    string hash = encoding.GetString(sha1.ComputeHash(encoding.GetBytes(hashString)));

                    //save the hash for validating later calls
                    _cache[hash] = profile;

                    //return the hash to the caller
                    return hash;
                }
            }
            return "";
        }
    }
}
