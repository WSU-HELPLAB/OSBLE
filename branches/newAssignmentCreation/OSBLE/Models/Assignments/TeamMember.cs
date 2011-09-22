using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;

namespace OSBLE.Models.Assignments
{
    public class TeamMember
    {
        [Key]
        [Column(Order=0)]
        public int TeamID { get; set; }
        public Team Team { get; set; }

        [Key]
        [Column(Order=1)]
        public int CourseUserID { get; set; }
        public CourseUsers CourseUser;
    }
}