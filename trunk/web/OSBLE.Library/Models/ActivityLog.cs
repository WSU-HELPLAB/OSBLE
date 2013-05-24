using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;

namespace OSBLE.Models
{
    public class ActivityLog
    {
        [Key]
        [Required]
        public int ID { get; set; }

        /// <summary>
        /// The object that is making the log
        /// </summary>
        public string Sender { get; set; }

        public int? UserID { get; set; }

        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The user that is making the request (usually the person logged in)
        /// </summary>
        [ForeignKey("UserID")]
        public virtual UserProfile User { get; set; }

        /// <summary>
        /// Log-specific message information
        /// </summary>
        public string Message { get; set; }

        public ActivityLog()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}