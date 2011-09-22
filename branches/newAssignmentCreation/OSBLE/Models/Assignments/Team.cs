using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class Team
    {
        [Key]
        public int ID { get; set; }
        public int Name { get; set; }

        [Association("AssignmentTeams_Teams", "ID", "TeamID")]
        public virtual IList<Deliverable> Deliverables { get; set; }
        public IList<TeamMember> TeamMembers { get; set; }

        public Team()
        {
            TeamMembers = new List<TeamMember>();
        }
    }
}