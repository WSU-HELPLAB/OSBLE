using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
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
        public int CourseReferenceID { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

    }
}