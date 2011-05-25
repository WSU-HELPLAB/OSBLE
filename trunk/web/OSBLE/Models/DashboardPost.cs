using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace OSBLE.Models
{
    public class DashboardPost
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        [Required]
        public int CourseID { get; set; }

        public virtual AbstractCourse Course { get; set; }

        public int UserProfileID { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        [AllowHtml]
        [Required]
        public string Content { get; set; }

        public virtual ICollection<DashboardReply> Replies { get; set; }

        public DashboardPost()
            : base()
        {
            Posted = DateTime.Now;
        }
    }
}