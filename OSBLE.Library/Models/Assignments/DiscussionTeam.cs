using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.DiscussionAssignment;
using System.ComponentModel.DataAnnotations.Schema;

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
                if( tm.CourseUser != null && !returnValHelper.ContainsKey(tm.CourseUserID))
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

        /// <summary>
        /// Returns the number of new posts for currentCourseUserID for this discussion
        /// </summary>
        /// <param name="currentCourseUserID"></param>
        /// <returns></returns>
        public int GetNewPostsCount(int currentCourseUserId)
        {
            int returnVal = 0;
            using (OSBLEContext db = new OSBLEContext())
            {
                if (Assignment.HasDiscussionTeams) //Filter by discussionTeamID
                {
                    DiscussionAssignmentMetaInfo dtmi = (from mi in db.DiscussionAssignmentMetaTable
                                                         where mi.CourseUserID == currentCourseUserId &&
                                                     mi.DiscussionTeamID == this.ID
                                                     select mi).FirstOrDefault();

                    //if dtmi is null, set LastVisit to MinValue so all posts look new
                    if(dtmi == null) 
                    {
                        dtmi = new DiscussionAssignmentMetaInfo();
                        dtmi.LastVisit = DateTime.MinValue;
                    }

                    returnVal = (from dp in db.DiscussionPosts
                                 where dp.Posted > dtmi.LastVisit &&
                                 dp.DiscussionTeamID == this.ID
                                 select dp).Count();
                }
                else //Discussion team ID could be any discussion team in the class. So, queries are a little different
                {
                    List<int> possibleDiscussionIDs = Assignment.DiscussionTeams.Select(dt => dt.ID).ToList();
                    DiscussionAssignmentMetaInfo dtmi = (from mi in db.DiscussionAssignmentMetaTable
                                                         where mi.CourseUserID == currentCourseUserId &&
                                                     possibleDiscussionIDs.Contains(mi.DiscussionTeamID)
                                                     select mi).FirstOrDefault();

                    //if dtmi is null, set LastVisit to MinValue so all posts look new
                    if (dtmi == null)
                    {
                        dtmi = new DiscussionAssignmentMetaInfo();
                        dtmi.LastVisit = DateTime.MinValue;
                    }

                    returnVal = (from dp in db.DiscussionPosts
                                 where dp.Posted > dtmi.LastVisit &&
                                 dp.AssignmentID == AssignmentID
                                 select dp).Count();
                }
            }
            return returnVal;
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