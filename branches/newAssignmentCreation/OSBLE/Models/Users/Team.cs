using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Users
{
    public class Team
    {
        public Team()
        {
            Members = new List<TeamUserMember>();
        }

        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public virtual ICollection<TeamUserMember> Members { get; set; }

        public bool Contains(UserProfile user)
        {
            foreach (TeamUserMember member in Members)
            {
                if (member.Contains(user))
                {
                    return true;
                }
            }
            return false;
        }
    }
}