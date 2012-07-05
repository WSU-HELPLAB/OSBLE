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
using System.Text;
using System.Web;

namespace OSBLE.Services
{
    /// <summary>
    /// </summary>
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class AuthenticationService
    {
        private FileCache _cache;
        private OSBLEContext _db = new OSBLEContext();

        private FileCache Cache
        {
            get
            {
                if (_cache == null)
                {
                    SetUpCache();
                }
                return _cache;
            }
        }

        public AuthenticationService()
        {
        }

        private void SetUpCache()
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
            UserProfile profile = Cache[authToken] as UserProfile;
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
            UserProfile profile = Cache[authToken] as UserProfile;
            if (profile == null)
            {
                return new UserProfile();
            }
            return new UserProfile(profile);
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
                    byte[] textBytes = Encoding.ASCII.GetBytes(hashString);
                    byte[] hashBytes = sha1.ComputeHash(textBytes);
                    string hashText = BitConverter.ToString(hashBytes);

                    //save the hash for validating later calls
                    Cache[hashText] = profile;

                    //return the hash to the caller
                    return hashText;
                }
            }
            return "";
        }
    }
}
