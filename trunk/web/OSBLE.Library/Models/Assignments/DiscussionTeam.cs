using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class DiscussionTeam : IAssignmentTeam
    {
        [Key]
        public int ID { get; set; }

        public int AssignmentID { get; set; }
        [ForeignKey("AssignmentID")]
        public virtual Assignment Assignment { get; set; }

        public int TeamID { get; set; }
        [ForeignKey("TeamID")]
        public virtual Team Team { get; set; }

        /// <summary>
        /// This will host the AuthorTeam for Critical Review Discussion Assignments. For regular Discussion Assignments, 
        /// this should be remain null.
        /// </summary>
        public int? AuthorTeamID { get; set; }

        [ForeignKey("AuthorTeamID")]
        public virtual Team AuthorTeam { get; set; }

        /// <summary>
        /// Returns all the distinct team members for the DiscussionTeam. 
        /// </summary>
        public List<TeamMember> GetAllTeamMembers()
        {
            Dictionary<int, TeamMember> returnValHelper = new Dictionary<int, TeamMember>();
            List<TeamMember> potentialMembers = Team.TeamMembers.ToList();

            if (AuthorTeamID != null)
            {
                potentialMembers.AddRange(AuthorTeam.TeamMembers.ToList());
            }

            foreach (TeamMember tm in potentialMembers)
            {
                if(!returnValHelper.ContainsKey(tm.CourseUserID))
                {
                    returnValHelper.Add(tm.CourseUserID, tm);
                }
            }

            return returnValHelper.Values.OrderBy(tm => tm.CourseUser.UserProfile.LastName).ThenBy(tm => tm.CourseUser.UserProfile.FirstName).ToList();
        }

        [NotMapped]
        public string TeamName
        {
            get
            {
                return Team.Name;
            }
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscussionTeam>()
                .HasRequired(dt => dt.Team)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiscussionTeam>()
                .HasRequired(dt => dt.Assignment)
                .WithMany(a => a.DiscussionTeams)
                .WillCascadeOnDelete(false);
        }
    }
}