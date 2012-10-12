using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class ReviewTeam : IReviewTeam, IModelBuilderExtender
    {
        [Key]
        [Column(Order = 0)]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        [Key]
        [Column(Order = 1)]
        public int AuthorTeamID { get; set; }
        public virtual Team AuthorTeam { get; set; }

        [Key]
        [Column(Order = 2)]
        public int ReviewTeamID { get; set; }

        [ForeignKey("ReviewTeamID")]
        public virtual Team ReviewingTeam { get; set; }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReviewTeam>()
                .HasRequired(rt => rt.ReviewingTeam)
                .WithMany()
                .WillCascadeOnDelete(false);


            modelBuilder.Entity<ReviewTeam>()
                .HasRequired(rt => rt.AuthorTeam)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }
}