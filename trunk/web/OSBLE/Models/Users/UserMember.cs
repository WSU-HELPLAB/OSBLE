using System.ComponentModel.DataAnnotations;

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
    }
}