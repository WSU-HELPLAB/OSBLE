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

namespace OSBLE.Services
{
    /// <summary>
    /// Authentication code was adapted from code in UploaderWebService.svc
    /// </summary>
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class AuthenticationService
    {
        private const double sessionTimeoutInMinutes = -15.0;
        private static Dictionary<string, UserSession> activeSessions = new Dictionary<string, UserSession>();
        private OSBLEContext db = new OSBLEContext();

        /// <summary>
        /// Call this to remove expired sessions.  This
        /// function will extend the keep alive timer on the supplied key.
        /// </summary>
        /// <param name="authToken">The key to extend.</param>
        private void CleanActiveSessions(string authToken)
        {

            //update the current key
            if (activeSessions.Keys.Contains(authToken))
            {
                activeSessions[authToken].LastAccessTime = DateTime.Now;
            }

            //call the normal clean function
            CleanActiveSessions();
        }

        /// <summary>
        /// Call this at the end of every request to remove expired sessions
        /// </summary>
        private void CleanActiveSessions()
        {
            //set an expiration time of 15 minutes ago
            DateTime expirationDate = DateTime.Now.AddMinutes(sessionTimeoutInMinutes);
            List<string> expiredKeys = new List<string>();
            foreach (string key in activeSessions.Keys)
            {
                //log any expired keys
                if (activeSessions[key].LastAccessTime < expirationDate)
                {
                    //note that we can't modify a collection that we are iterating through
                    //so we must add to another collection first
                    expiredKeys.Add(key);
                }
            }

            //remove expired keys
            foreach (string key in expiredKeys)
            {
                activeSessions.Remove(key);
            }
        }

        /// <summary>
        /// Will tell you if the supplied key is valid
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public bool IsValidKey(string authToken)
        {
            //clean our session list
            CleanActiveSessions();

            //after cleaning, all remaining keys should be valid
            if (activeSessions.Keys.Contains(authToken))
            {
                //if the key exists, might as well update it as well
                activeSessions[authToken].LastAccessTime = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
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
            if (!IsValidKey(authToken))
            {
                return new UserProfile();
            }
            return activeSessions[authToken].UserProfile;
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
            //clean up the active sessions table
            CleanActiveSessions();

            if (Membership.ValidateUser(userName, password))
            {
                UserProfile profile = (from p in db.UserProfiles
                                       where p.AspNetUserName == userName
                                       select p).First();

                //formally log the user into OSBLE
                FormsAuthentication.SetAuthCookie(userName, false);

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
                    activeSessions.Add(hash, new UserSession(profile));

                    //return the hash to the caller
                    return hash;
                }
            }
            return "";
        }
    }
}
