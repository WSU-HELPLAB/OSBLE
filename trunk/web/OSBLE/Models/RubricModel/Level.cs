using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.RubricModel
{
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

}