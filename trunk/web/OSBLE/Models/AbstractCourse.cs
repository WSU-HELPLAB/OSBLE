﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public abstract class AbstractCourse
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Course Name")]
        public string Name { get; set; }

    }
}