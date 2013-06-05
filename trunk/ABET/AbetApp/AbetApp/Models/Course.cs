using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AbetApp.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public int CourseNum { get; set; }
        public string Data { get; set; }
        public string Major { get; set; }
        public string Outcomes { get; set; }
        public string Year { get; set; }
        public string Semester { get; set; }
    }
}