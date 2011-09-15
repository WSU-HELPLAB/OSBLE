﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses.Rubrics
{
    public class Level
    {
        [Required]
        [Key]
        [Editable(true)]
        public int ID { get; set; }

        [Required]
        public int RubricID { get; set; }

        public virtual Rubric Rubric { get; set; }

        [Required]
        public int RangeStart { get; set; }

        [Required]
        public int RangeEnd { get; set; }

        [Required]
        public string LevelTitle { get; set; }

        public Level()
            : base()
        {
            RangeStart = 0;
        }

    }

}