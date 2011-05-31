using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace OSBLE.Models.Users
{
    public class Mail
    {
        [Required]
        [Key]
        public int ID {get; set;}

        [Required]
        public int FromUserProfileID { get; set; }
        public virtual UserProfile FromUserProfile { get; set; }

        [Required]
        public int ToUserProfileID { get; set; }
        public virtual UserProfile ToUserProfile { get; set; }

        [Required]
        public bool Read { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        [Required]
        [AllowHtml]
        public string Subject { get; set; }

        [Required]
        [AllowHtml]
        public string Message { get; set; }

        public Mail()
            : base()
        {
            Posted = DateTime.Now;
        }

    }
}