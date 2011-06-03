using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class Rubric
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public string Description { get; set; }

    }

    public class Level
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int RubricID { get; set; }

        [Required]
        public int RangeStart { get; set; }

        [Required]
        public int RangeEnd { get; set; }

        [Required]
        public string LevelTitle { get; set; }

    }

    public class Criteria
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int RubricID { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string CriterionTitle { get; set; }

    }

    public class LevelDescription
    {
        [Required]
        [Key]
        public int CriterionID { get; set; }

        [Required]
        [Key]
        public int LevelID { get; set; }

        [Required]
        public string Description { get; set; }

    }
}