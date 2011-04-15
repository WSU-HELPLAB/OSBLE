using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OSBLE.Models {

    public class CoursesUsers
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        [Column(Order = 0)]
        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }


        [Required]
        [Column(Order=1)]
        public int CourseID { get; set; }
        public virtual Course Course { get; set; }

        [Required]
        public int CourseRoleID { get; set; }
        public virtual CourseRole CourseRole { get; set; }

        [Required]
        public int Section { get; set; }
    }
}