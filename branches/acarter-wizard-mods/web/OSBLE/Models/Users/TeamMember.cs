using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Users
{
    public class OldTeamMember : TeamUserMember
    {
        [Required]
        public int TeamID { get; set; }

        public virtual OldTeam Team { get; set; }

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