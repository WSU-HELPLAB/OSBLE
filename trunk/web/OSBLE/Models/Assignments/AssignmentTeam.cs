using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class AssignmentTeam
    {
        [Key]
        [Column(Order=0)]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        [Key]
        [Column(Order=0)]
        public int TeamID { get; set; }

        [ForeignKey("TeamID")]
        public virtual Team Team { get; set; }
    }
}