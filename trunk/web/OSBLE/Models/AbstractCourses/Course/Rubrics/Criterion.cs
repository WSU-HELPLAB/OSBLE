using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses.Rubrics
{
    public class Criterion
    {
        [Required]
        [Key]
        [Editable(true)]
        public int ID { get; set; }

        [Required]
        public int RubricID { get; set; }

        public virtual Rubric Rubric { get; set; }

        [Required]
        public string CriterionTitle { get; set; }

        [Required]
        public double Weight { get; set; }

    }
}