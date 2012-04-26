using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Users;
using System.Web.Security;
using System.IO;
using OSBLE.Models;

namespace OSBLE.Utility
{
    public class OsbleAuthentication
    {
        public static string ProfileCookieKey
        {
            get
            {
                return "osble_profile";
            }
        }
        private const string userNameKey = "UserName";
        private const string authKey = "AuthKey";

        private OSBLEContext db = new OSBLEContext();

        public static string Encrypt(string content)
        {
            byte[] rawBytes = System.Text.Encoding.UTF8.GetBytes(content);
            string enctryptedString = MachineKey.Encode(rawBytes, MachineKeyProtection.All); 
            return enctryptedString;
        }

        public static string Decrypt(string content)
        {
            byte[] encryptedBytes = MachineKey.Decode(content, MachineKeyProtection.All);
            string decryptedContent = System.Text.Encoding.UTF8.GetString(encryptedBytes);
            return decryptedContent;
        }

        /// <summary>
        /// Logs the supplied user into OSBLE
        /// </summary>
        /// <param name="profile"></param>
        public static void LogIn(UserProfile profile)
        {
            HttpCookie cookie = new HttpCookie(ProfileCookieKey);

            //save the profile's user name
            cookie.Values[userNameKey] = Encrypt(profile.UserName);

            //set a really long expiration date
            cookie.Expires = DateTime.Now.AddDays(300);

            //and then store it in the next response
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Response.Cookies.Set(cookie);
            }
        }

        /// <summary>
        /// Returns the currently logged in user
        /// </summary>
        public static UserProfile CurrentUser
        {
            get
            {
                UserProfile profile = null;
                if (HttpContext.Current != null)
                {
                    OSBLEContext db = new OSBLEContext();
                    try
                    {
                        HttpCookie cookie = HttpContext.Current.Request.Cookies.Get(ProfileCookieKey);
                        string userName = Decrypt(cookie.Values[userNameKey]);
                        return db.UserProfiles.Where(u => u.UserName == userName).FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("Error parsing current user for IP {0}: {1}", HttpContext.Current.Request.UserHostAddress, ex.Message);
                        ActivityLog log = new ActivityLog()
                        {
                            Sender = typeof(OsbleAuthentication).ToString(),
                            Message = message
                        };
                        db.ActivityLogs.Add(log);
                        db.SaveChanges();
                    }
                }
                return profile;
            }
        }

        /// <summary>
        /// Logs the current user out of the system
        /// </summary>
        public static void LogOut()
        {
            if (HttpContext.Current != null)
            {
                //cookie might not exist
                try
                {
                    HttpCookie cookie = HttpContext.Current.Request.Cookies.Get(ProfileCookieKey);
                    cookie.Expires = DateTime.Now.AddDays(-1d);
                    HttpContext.Current.Response.Cookies.Set(cookie);
                }
                catch (Exception ex)
                {
                    //removes annoying VS warning message
                    string foo = ex.Message;
                }
            }
        }
    }
}