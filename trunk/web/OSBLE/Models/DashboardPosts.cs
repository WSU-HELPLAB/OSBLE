using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        public virtual Course Course { get; set; }

        public int UserProfileID { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        public string Content { get; set; }

        public virtual ICollection<DashboardPost> Replies { get; set; }

        public DashboardPost()
            : base()
        {
            Posted = DateTime.Now;
        }
    }
}