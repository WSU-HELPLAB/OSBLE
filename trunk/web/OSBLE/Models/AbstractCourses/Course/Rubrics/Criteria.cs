using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.RubricModel
{
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
}