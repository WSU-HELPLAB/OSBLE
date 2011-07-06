using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses.Rubrics
{
    public class CriterionEvaluation
    {
        [Key]
        [Required]
        public int RubricEvaluationID { get; set; }
        public virtual RubricEvaluation RubricEvaluation { get; set; }

        [Key]
        [Required]
        public int CriterionID { get; set; }
        public Criterion Criterion { get; set; }

        public int? Score { get; set; }

        [MaxLength(4000)]
        public string? Comment { get; set; }
    }
}