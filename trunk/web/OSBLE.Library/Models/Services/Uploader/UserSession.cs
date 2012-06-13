using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Users;
namespace OSBLE.Models.Services.Uploader
{
    [Serializable]
    public class UserSession
    {
        public DateTime LastAccessTime
        {
            get;
            set;
        }

        public UserProfile UserProfile
        {
            get;
            set;
        }

        public UserSession(UserProfile profile)
        {
            this.UserProfile = new UserProfile(profile);
            LastAccessTime = DateTime.Now;
        }
    }
}