using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Users
{
    public class Team
    {
        public Team()
        {
            Members = new List<TeamMember>();
        }

        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public ICollection<TeamMember> Members { get; set; }

        public bool Contains(UserProfile user)
        {
            foreach (TeamMember member in Members)
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