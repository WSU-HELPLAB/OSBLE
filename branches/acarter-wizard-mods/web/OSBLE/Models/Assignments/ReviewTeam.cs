using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class AssignmentReviewTeam
    {
        [Key]
        [Column(Order = 0)]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        [Key]
        [Column(Order = 1)]
        public int ReviewTeamID { get; set; }

        /// <summary>
        /// The team that will be reviewing the author's work
        /// </summary>
        [ForeignKey("ReviewTeamID")]
        public virtual Team ReviewTeam { get; set; }

        [Key]
        [Column(Order = 2)]
        public int AuthorTeamID { get; set; }

        /// <summary>
        /// The original author of the document to be reviewed
        /// </summary>
        [ForeignKey("AuthorTeamID")]
        public virtual Team AuthorTeam { get; set; }
    }
}