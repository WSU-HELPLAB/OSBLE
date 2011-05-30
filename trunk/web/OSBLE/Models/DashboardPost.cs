using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace OSBLE.Models
{
    public class DashboardPost : AbstractDashboard
    {
        [Required]
        public int CourseID { get; set; }

        public virtual AbstractCourse Course { get; set; }

        public bool CanReply { get; set; }

        public virtual ICollection<DashboardReply> Replies { get; set; }

        public DashboardPost()
            : base()
        {

        }
    }
}