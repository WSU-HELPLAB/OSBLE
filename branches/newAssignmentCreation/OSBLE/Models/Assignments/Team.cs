using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class Team : IComparable
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }

        [Association("AssignmentTeam_Team", "ID", "TeamID")]
        public IList<TeamMember> TeamMembers { get; set; }

        public Team()
        {
            TeamMembers = new List<TeamMember>();
        }

        int IComparable.CompareTo(object obj)
        {
            Team other = obj as Team;
            if (other == null)
            {
                return -1;
            }
            if (other.ID == 0 || this.ID == 0)
            {
                return this.Name.CompareTo(other.Name);
            }
            else
            {
                return this.ID.CompareTo(other.ID);
            }
        }
    }
}