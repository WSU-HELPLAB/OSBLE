using System.ComponentModel.DataAnnotations;
using System;

namespace OSBLE.Models.Users
{
    public class UserMember : TeamUserMember
    {
        [Required]
        public int UserProfileID { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public override bool Contains(UserProfile user)
        {
            return UserProfileID == user.ID;
        }

        public override string GetName()
        {
            return String.Format("{0}, {1}", UserProfile.LastName, UserProfile.FirstName);
        }

        public override string ToString()
        {
            return UserProfile.ToString();
        }
    }
}