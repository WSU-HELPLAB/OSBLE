using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace AbetApp.Models
{
    public class CourseRelation
    {
        [Key]
        [Column(Order=0)]
        public int ParentCourseId { get; set; }

        [Key]
        [Column(Order=1)]
        public int ChildCourseId { get; set; }
    }
}