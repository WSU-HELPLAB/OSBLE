using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class DiscussionTeam : IAssignmentTeam
    {
        [Key]
        public int ID { get; set; }

        public int AssignmentID { get; set; }
        [ForeignKey("AssignmentID")]
        public virtual Assignment Assignment { get; set; }

        public int TeamID { get; set; }
        [ForeignKey("TeamID")]
        public virtual Team Team { get; set; }

        /// <summary>
        /// This will host the AuthorTeam for Critical Review Discussion Assignments. For regular Discussion Assignments, 
        /// this should be remain null.
        /// </summary>
        public int? AuthorTeamID { get; set; }
        [ForeignKey("AuthorTeamID")]
        public virtual Team AuthorTeam { get; set; }

        /// <summary>
        /// Returns all the team members for the DiscussionTeam. 
        /// </summary>
        public List<TeamMember> GetAllTeamMembers()
        {
            List<TeamMember> returnVal;

            returnVal = Team.TeamMembers.ToList();
            if (AuthorTeam != null)
            {
                returnVal.AddRange(AuthorTeam.TeamMembers.ToList());
            }

            return returnVal;
        }

    }
}