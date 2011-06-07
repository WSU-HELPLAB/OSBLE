using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Rubrics
{
    
    public class CellDescription
    {
        [Required]
        [Key]
        [Column(Order = 0)]
        public int CriterionID { get; set; }

        public virtual Criterion Criterion { get; set; }

        [Required]
        [Key]
        [Column(Order = 1)]
        public int LevelID { get; set; }

        public virtual Level Level { get; set; }

        [Required]
        public string Description { get; set; }
    }
}