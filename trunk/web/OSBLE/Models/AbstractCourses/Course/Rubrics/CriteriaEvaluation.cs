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
        [Column(Order = 0)]
        public int RubricEvaluationID { get; set; }
        public virtual RubricEvaluation RubricEvaluation { get; set; }

        [Key]
        [Required]
        [Column(Order = 1)]
        public int CriterionID { get; set; }
        public Criterion Criterion { get; set; }

        public int? Score { get; set; }

        [StringLength(4000)]
        public string Comment { get; set; }
    }
}