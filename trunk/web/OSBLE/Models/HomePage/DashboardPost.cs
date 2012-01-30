using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using System;

namespace OSBLE.Models.HomePage
{
    public class DashboardPost : AbstractDashboard
    {
        public bool CanReply { get; set; }

        public virtual ICollection<DashboardReply> Replies { get; set; }

        [Obsolete("For legacy compatibility.  You should be using the CourseUser property instead.")]
        public int CourseID
        {
            get
            {
                if (this.CourseUser != null)
                {
                    return this.CourseUser.AbstractCourseID;
                }
                return 0;
            }
        }

        [Obsolete("For legacy compatibility.  You should be using the CourseUser property instead.")]
        public Course Course
        {
            get
            {
                return this.CourseUser.AbstractCourse as Course;
            }
        }

        public DashboardPost()
            : base()
        {
        }
    }
}