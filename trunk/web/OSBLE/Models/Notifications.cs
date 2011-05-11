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
        public int RecipientID { get; set; }

        public UserProfile Recipient { get; set; }

        public int SenderID { get; set; }

        public UserProfile Sender { get; set; }

        [Required]
        public int CourseID { get; set; }

        public Course Course { get; set; }

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