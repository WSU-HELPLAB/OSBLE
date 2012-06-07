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
    public class DiscussionRepliesTableDecorator : TableDecorator
    {
        List<DiscussionPost> AllUserPosts { get; set; }

        public DiscussionRepliesTableDecorator(ITableBuilder builder, List<DiscussionPost> allUserPosts)
            : base(builder)
        {
            AllUserPosts = allUserPosts;
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam discussionTeam)
        {
            dynamic data = Builder.BuildTableForTeam(discussionTeam);
            data.Replies = new DynamicDictionary();

            TeamMember member = discussionTeam.Team.TeamMembers.FirstOrDefault();
            data.Replies.PostCount = 0;
            if (member != null)
            {
                data.Replies.PostCount = (from a in AllUserPosts
                              where a.CourseUserID == member.CourseUserID &&
                              a.DiscussionTeamID == (discussionTeam as DiscussionTeam).ID &&
                              a.IsReply
                              select a).Count();
            }

            data.Replies.CourseUserID = member.CourseUserID;
            data.Replies.AssignmentID = discussionTeam.AssignmentID;
            data.Replies.DiscussionTeamID = (discussionTeam as DiscussionTeam).ID;

            return data;
        }
    }
}