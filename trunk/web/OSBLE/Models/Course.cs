﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Course
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Course Prefix")]
        public string Prefix { get; set; }

        [Required]
        [Display(Name = "Course Number")]
        public string Number { get; set; }

        [Required]
        [Display(Name = "Course Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Semester")]
        public string Semester { get; set; }

        [Required]
        [Display(Name = "Year")]
        public string Year { get; set; }

        [Display(Name = "Course Weight")]
        public ICollection<Weight> Weights { get; set; }
    }
}