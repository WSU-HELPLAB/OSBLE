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
        private OSBLEContext db = new OSBLEContext();

        public UserProfile GetUserFromCookie(HttpCookie cookie)
        {
            UserProfile profile = new UserProfile();
            byte[] bytes = MachineKey.Decode(cookie.Values[userNameKey].ToString(), MachineKeyProtection.All);
            string userName = System.Text.Encoding.UTF8.GetString(bytes);
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

            //set a really long expiration date
            cookie.Expires = DateTime.Now.AddDays(30);
            return cookie;
        }
    }
}