using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Users
{
    public class TeamMember : TeamUserMember
    {
        [Required]
        public int TeamID { get; set; }

        public virtual Team Team { get; set; }

        public override bool Contains(UserProfile user)
        {
            return Team.Contains(user);
        }

        public override string GetName()
        {
            return Team.Name;
        }
    }
}