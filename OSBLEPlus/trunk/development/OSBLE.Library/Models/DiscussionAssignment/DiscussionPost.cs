using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.DiscussionAssignment
{
    public class DiscussionPost : IModelBuilderExtender
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        [Required]
        public int CourseUserID { get; set; }
        public virtual CourseUser CourseUser { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        public int? ParentPostID { get; set; }
        public virtual DiscussionPost ParentPost { get; set; }

        public virtual ICollection<DiscussionPost> Replies { get; set; }

        [NotMapped]
        public bool IsReply { 
            get 
            {
                return (this.ParentPostID != null);
            } 
        }

        [Required]
        public int DiscussionTeamID { get; set; }
        [ForeignKey("DiscussionTeamID")]
        public virtual DiscussionTeam DiscussionTeam { get; set; }

        public DiscussionPost()
            : base()
        {
            ParentPostID = null;
            Posted = DateTime.UtcNow;
        }

        public DiscussionPost(DiscussionPost src) : this()
        {
            AssignmentID = src.AssignmentID;
            CourseUserID = src.CourseUserID;
            DiscussionTeamID = src.DiscussionTeamID;
            ParentPostID = src.ParentPostID;
            Content = src.Content;
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscussionPost>()
                .HasRequired(cu => cu.CourseUser)
                .WithMany()
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<DiscussionPost>()
                .HasRequired(n => n.DiscussionTeam)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiscussionPost>()
                .HasRequired(m => m.CourseUser)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }
}