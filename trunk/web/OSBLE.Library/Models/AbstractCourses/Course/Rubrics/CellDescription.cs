using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses.Rubrics
{
    
    public class CellDescription
    {
        [Required]
        [Key]
        [Column(Order = 0)]
        public int CriterionID { get; set; }

        [Association("Criterion", "CriterionID", "ID")]
        public virtual Criterion Criterion { get; set; }

        [Required]
        [Key]
        [Column(Order = 1)]
        public int LevelID { get; set; }

        [Association("Level", "LevelID", "ID")]
        public virtual Level Level { get; set; }

        [Required]
        public string Description { get; set; }
    }
}