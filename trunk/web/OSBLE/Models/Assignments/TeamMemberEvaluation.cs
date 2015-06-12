using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;

namespace OSBLE.Models.Assignments
{
    public class TeamMemberEvaluation
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int EvaluatorID { get; set; }
        public virtual CourseUser Evaluator { get; set; }

        [Required]
        public int RecipientID { get; set; }
        public virtual CourseUser Recipient { get; set; }

        [Required]
        public int TeamEvaluationID { get; set; }
        public virtual TeamEvaluation TeamEvaluation { get; set; }

        [Required]
        public int Points { get; set; }
    }
}