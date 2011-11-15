using System.Collections.Generic;
using System.Linq;
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

        [Required(ErrorMessage="A team name is required")]
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

        public void Remove(UserProfile user)
        {
            //the user should only be on the team once
            TeamUserMember memberToRemove = (from member in Members
                                             where member is UserMember
                                             && (member as UserMember).UserProfileID == user.ID
                                             select member).FirstOrDefault();
            Members.Remove(memberToRemove);

            //recursively make the call for all TeamMembers as well
            var query = from member in Members
                        where member is TeamMember
                        select member as TeamMember;
            foreach (var item in query)
            {
                item.Team.Remove(user);
            }

        }
    }
}