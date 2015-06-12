using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class DiscussionTotalTableDecorator : TableDecorator
    {
        List<DiscussionPost> AllUserPosts { get; set; }
        public DiscussionTotalTableDecorator(ITableBuilder builder, List<DiscussionPost> allUserPosts)
            : base(builder)
        {
            AllUserPosts = allUserPosts;
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam discussionTeam)
        {
            dynamic data = Builder.BuildTableForTeam(discussionTeam);
            data.Total = new DynamicDictionary();

            TeamMember member = discussionTeam.Team.TeamMembers.FirstOrDefault();
            data.Total.PostCount = 0;
            if (member != null)
            {
                data.Total.PostCount = (from a in AllUserPosts
                                  where a.CourseUserID == member.CourseUserID &&
                                  a.DiscussionTeamID == (discussionTeam as DiscussionTeam).ID &&
                                  !a.IsReply
                                  select a).Count();

                data.Total.PostCount += (from a in AllUserPosts
                                  where a.CourseUserID == member.CourseUserID &&
                                  a.DiscussionTeamID == (discussionTeam as DiscussionTeam).ID &&
                                  a.IsReply
                                  select a).Count();
            }

            data.Total.CourseUserID = member.CourseUserID;
            data.Total.AssignmentID = discussionTeam.AssignmentID;
            data.Total.DiscussionTeamID = (discussionTeam as DiscussionTeam).ID;
            return data;
        }
    }
}