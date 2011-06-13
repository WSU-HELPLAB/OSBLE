using System;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Models.HomePage
{
    public class Notification
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int RecipientID { get; set; }

        public virtual UserProfile Recipient { get; set; }

        public int? SenderID { get; set; }

        public virtual UserProfile Sender { get; set; }

        public int? CourseID { get; set; }

        public virtual AbstractCourse Course { get; set; }

        public bool Read { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        // Multiple items can be used in a notification, including Assignments, Grades, etc.
        // They are defined in the Types class at the bottom of this model.
        public string ItemType { get; set; }

        public int ItemID { get; set; }

        [NotMapped]
        public string Message { get; set; }

        public Notification()
            : base()
        {
            Posted = DateTime.Now;
            Read = false;
        }

        public static class Types
        {
            public const string Mail = "Mail";
            public const string EventApproval = "EventApproval";
            public const string Dashboard = "Dashboard";
        }
    }
}