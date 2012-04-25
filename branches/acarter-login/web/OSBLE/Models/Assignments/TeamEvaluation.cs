using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class TeamEvaluation
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        public int TeamID { get; set; }
        public virtual Team Team { get; set; }

        public string Comments { get; set; }

        [Association("TeamMemberEvaluation_TeamEvaluation", "ID", "TeamEvaluationID")]
        public virtual IList<TeamMemberEvaluation> TeamMemberEvaluations { get; set; }
    }
}