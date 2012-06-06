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
        [Column(Order = 0)]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        [Key]
        [Column(Order = 1)]
        public int TeamID { get; set; }

        [ForeignKey("TeamID")]
        public virtual Team Team { get; set; }


        //This will host the AuthorTeam for Critical Review Discussion Assignments. For regular Discussion Assignments, 
        //this should be remain null.
        public int? AuthorTeamID { get; set; }
        [ForeignKey("AuthorTeamID")]
        public virtual Team AuthorTeam { get; set; }

    }
}