using System;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Notifications
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int UserProfileID { get; set; }

        public UserProfile UserProfile { get; set; }

        public bool Read { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        // Multiple items can be used in a notification, including Assignments, Grades, etc.
        public string ItemType { get; set; }

        public int ItemID { get; set; }

        public string Message { get; set; }

        public Notifications()
            : base()
        {
            Posted = DateTime.Now;
        }
    }
}