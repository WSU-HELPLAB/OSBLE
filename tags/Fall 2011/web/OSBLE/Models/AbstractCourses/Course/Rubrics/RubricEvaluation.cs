using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;
using OSBLE.Models.Users;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Models.Courses.Rubrics
{
    public class RubricEvaluation
    {
        public RubricEvaluation()
        {
            CriterionEvaluations = new List<CriterionEvaluation>();
            IsPublished = false;
        }

        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public int EvaluatorID { get; set; }

        public virtual UserProfile Evaluator { get; set; }

        [Required]
        public int RecipientID { get; set; }

        public virtual TeamUserMember Recipient { get; set; }

        [Required]
        public int AbstractAssignmentActivityID { get; set; }

        public virtual AbstractAssignmentActivity AssignmentActivity { get; set; }

        [Required]
        public bool IsPublished { get; set; }

        public DateTime? DatePublished { get; set; }

        [StringLength(4000)]
        public string GlobalComment { get; set; }

        [Required]
        public virtual ICollection<CriterionEvaluation> CriterionEvaluations { get; set; }
    }
}