using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace OSBLE.Models
{
    public class DashboardReply
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        public int UserProfileID { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        [AllowHtml]
        [Required]
        public string Content { get; set; }

        public virtual DashboardPost Parent { get; set; }

        public DashboardReply()
            : base()
        {
            Posted = DateTime.Now;
        }
    }
}