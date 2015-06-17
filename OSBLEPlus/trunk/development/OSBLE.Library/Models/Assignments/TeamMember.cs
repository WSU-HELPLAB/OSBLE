using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.Assignments
{
    [Serializable]
    public class TeamMember : IModelBuilderExtender
    {
        public TeamMember()
        {
            
        }

        public TeamMember(TeamMember other)
        {
            this.CourseUserID = other.CourseUserID;
            this.CourseUser = other.CourseUser;
            this.TeamID = other.TeamID;
            this.Team = other.Team;
        }
        [Key]
        [Column(Order=0)]
        public int TeamID { get; set; }
        public virtual Team Team { get; set; }

        [Key]
        [Column(Order=1)]
        public int CourseUserID { get; set; }
        public virtual CourseUser CourseUser { get; set; }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamMember>()
                .HasRequired(tm => tm.CourseUser)
                .WithMany(cu => cu.TeamMemberships)
                .WillCascadeOnDelete(false);
        }
    }
}