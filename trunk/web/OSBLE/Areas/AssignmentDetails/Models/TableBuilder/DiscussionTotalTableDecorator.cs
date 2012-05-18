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

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            TeamMember member = assignmentTeam.Team.TeamMembers.FirstOrDefault();
            int postCount = 0;
            int replyCount = 0;
            if (member != null)
            {
                postCount = (from a in AllUserPosts
                                  where a.CourseUserID == member.CourseUserID &&
                                  !a.IsReply
                                  select a).Count();
                postCount = (from a in AllUserPosts
                                  where a.CourseUserID == member.CourseUserID &&
                                  !a.IsReply
                                  select a).Count();
                data.TotalCount = postCount + replyCount;
            }
            else
            {
                data.TotalCount = 0;
            }
            return data;
        }
    }
}