using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.RubricModel
{
    public class LevelDescription
    {
        [Required]
        [Key]
        [Column(Order = 0)]
        public int CriterionID { get; set; }

        [Required]
        [Key]
        [Column(Order = 1)]
        public int LevelID { get; set; }

        [Required]
        public string Description { get; set; }

    }
}