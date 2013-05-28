using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.AbstractCourses
{
    public class CourseRubric
    {
        [Required]
        [Key]
        [Column(Order=0)]
        public int AbstractCourseID { get; set; }
        public virtual AbstractCourse AbstractCourse { get; set; }

        [Required]
        [Key]
        [Column(Order=1)]
        public int RubricID { get; set; }
        public virtual Rubric Rubric { get; set; }
    }
}