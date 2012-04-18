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
            
            //user name
            byte[] bytes = MachineKey.Decode(cookie.Values[userNameKey].ToString(), MachineKeyProtection.All);
            string userName = System.Text.Encoding.UTF8.GetString(bytes);

            //auth key
            string authToken = System.Text.Encoding.UTF8.GetString(MachineKey.Decode(cookie.Values[authKey].ToString(), MachineKeyProtection.All));

            if (authToken.CompareTo(HttpContext.Current.Request.UserAgent) != 0)
            {
                return null;
            }

            profile = db.UserProfiles.Where(u => u.AspNetUserName == userName).FirstOrDefault();
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
            cookie.Values[authKey] = MachineKey.Encode(System.Text.Encoding.UTF8.GetBytes(HttpContext.Current.Request.UserAgent), MachineKeyProtection.All);

            //set a really long expiration date
            cookie.Expires = DateTime.Now.AddDays(30);
            return cookie;
        }
    }
}