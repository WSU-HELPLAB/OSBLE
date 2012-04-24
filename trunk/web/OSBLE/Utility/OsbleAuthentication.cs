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

        public UserProfile GetUserFromCookie(HttpCookie cookie)
        {
            UserProfile profile = new UserProfile();
            
            //do everything in a try/catch to account for potential null fields
            try
            {
                ActivityLog log = new ActivityLog()
                {
                    Sender = typeof(OsbleAuthentication).ToString(),
                    Message = "Attempting to retrieve cookie from IP " + HttpContext.Current.Request.UserHostAddress
                };
                db.ActivityLogs.Add(log);

                //user name
                byte[] bytes = MachineKey.Decode(cookie.Values[userNameKey].ToString(), MachineKeyProtection.All);
                string userName = System.Text.Encoding.UTF8.GetString(bytes);
                string authToken = System.Text.Encoding.UTF8.GetString(MachineKey.Decode(cookie.Values[authKey].ToString(), MachineKeyProtection.All));

                if (authToken.CompareTo(HttpContext.Current.Request.UserHostAddress) != 0)
                {
                    log = new ActivityLog()
                    {
                        Sender = typeof(OsbleAuthentication).ToString(),
                        Message = "Bad auth token detected.  Expected: " + HttpContext.Current.Request.UserHostAddress + ", Received: " + authToken
                    };
                    db.ActivityLogs.Add(log);
                    return null;
                }
                profile = db.UserProfiles.Where(u => u.AspNetUserName == userName).FirstOrDefault();
                
            }
            catch (Exception ex)
            {
                ActivityLog log = new ActivityLog()
                {
                    Sender = typeof(OsbleAuthentication).ToString(),
                    Message = "Authentiation exception encoutered for IP " + HttpContext.Current.Request.UserHostAddress + ": " + ex.Message
                };
                db.ActivityLogs.Add(log);
                db.SaveChanges();
                return null;
            }
            ActivityLog successLog = new ActivityLog()
            {
                Sender = typeof(OsbleAuthentication).ToString(),
                Message = "Authentication successful.",
                UserID = profile.ID
            };
            db.ActivityLogs.Add(successLog);
            db.SaveChanges();
            return profile;
        }

        public HttpCookie InvalidateUserCookie(UserProfile profile)
        {
            HttpCookie cookie = UserAsCookie(profile);
            cookie.Expires = DateTime.Now.AddDays(-1d);
            return cookie;
        }

        public HttpCookie UserAsCookie(UserProfile profile)
        {
            HttpCookie cookie = new HttpCookie(ProfileCookieKey);
            
            //encode and save the profile's user name
            cookie.Values[userNameKey] = MachineKey.Encode(System.Text.Encoding.UTF8.GetBytes(profile.AspNetUserName), MachineKeyProtection.All);

            //AC: need a better way to tie the cookie to the current machine
            cookie.Values[authKey] = MachineKey.Encode(System.Text.Encoding.UTF8.GetBytes(HttpContext.Current.Request.UserHostAddress), MachineKeyProtection.All);

            //set a really long expiration date
            cookie.Expires = DateTime.Now.AddDays(300);
            return cookie;
        }
    }
}