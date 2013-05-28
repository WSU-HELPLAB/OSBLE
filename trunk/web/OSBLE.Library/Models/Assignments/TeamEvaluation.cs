using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.Assignments
{
    public class TeamEvaluation : IModelBuilderExtender
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int EvaluatorID { get; set; }
        public virtual CourseUser Evaluator { get; set; }

        [Required]
        public int RecipientID { get; set; }
        public virtual CourseUser Recipient { get; set; }

        [Required]
        public int TeamEvaluationAssignmentID { get; set; }
        public virtual Assignment TeamEvaluationAssignment { get; set; }

        [Required]
        public int AssignmentUnderReviewID { get; set; }
        public virtual Assignment AssignmentUnderReview { get; set; }

        [Required]
        public int Points { get; set; }

        public int CommentID { get; set; }

        [ForeignKey("CommentID")]
        public virtual TeamEvaluationComment CommentObject { get; set; }

        public string Comment
        {
            get
            {
                string comment = "";
                if (CommentObject != null)
                {
                    comment = CommentObject.ToString();
                }
                return comment;
            }
            set
            {
                
            }
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.Evaluator)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.Recipient)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.AssignmentUnderReview)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.TeamEvaluationAssignment)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.Evaluator)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.Recipient)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }
}