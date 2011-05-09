using System;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class DashboardPosts
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        public int UserProfileID { get; set; }

        public UserProfile UserProfile { get; set; }

        public int ParentID { get; set; }

        public DashboardPosts Parent { get; set; }

        public DashboardPosts()
            : base()
        {
            Posted = DateTime.Now;
        }
    }
}