using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace OSBLE.Models.Assignments
{
    public class AbetAssignmentOutcome
    {
        [Key]
        [Column(Order=0)]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        [Key]
        [Column(Order = 1)]
        public string Outcome { get; set; }
    }
}
