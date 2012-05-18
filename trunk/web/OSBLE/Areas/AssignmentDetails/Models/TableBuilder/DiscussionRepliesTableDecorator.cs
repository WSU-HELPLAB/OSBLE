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

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);

            TeamMember member = assignmentTeam.Team.TeamMembers.FirstOrDefault();
            if (member != null)
            {
                data.replyCount = (from a in AllUserPosts
                              where a.CourseUserID == member.CourseUserID &&
                              a.IsReply
                              select a).Count();
            }
            else
            {
                data.postCount = 0;
            }

            return data;
        }
    }
}