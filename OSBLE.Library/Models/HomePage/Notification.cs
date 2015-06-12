using System;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Models.HomePage
{
    public class Notification : IModelBuilderExtender
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int RecipientID { get; set; }

        public virtual CourseUser Recipient { get; set; }

        public int? SenderID { get; set; }

        public virtual CourseUser Sender { get; set; }

        public bool Read { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        // Multiple items can be used in a notification, including Assignments, Grades, etc.
        // They are defined in the Types class at the bottom of this model.
        public string ItemType { get; set; }

        public int ItemID { get; set; }

        [StringLength(1000)]
        public string Data { get; set; }

        public Notification()
            : base()
        {
            Posted = DateTime.UtcNow;
            Read = false;
        }

        [Obsolete("For legacy compatibility.  You should be using either the Recipient or Sender property instead.")]
        public int CourseID
        {
            get
            {
                if (this.Recipient != null)
                {
                    return this.Recipient.AbstractCourseID;
                }
                return 0;
            }
        }

        public static class Types
        {
            public const string Mail = "Mail";
            public const string EventApproval = "EventApproval";
            public const string Dashboard = "Dashboard";
            public const string InlineReviewCompleted = "InlineReviewCompleted";
            public const string RubricEvaluationCompleted = "RubricEvaluationCompleted";
            public const string FileSubmitted = "FileSubmitted";
            public const string TeamEvaluationDiscrepancy = "TeamEvaluationDiscrepancy";
            public const string JoinCourseApproval = "JoinCourseApproval";
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.Sender)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.Recipient)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }
}