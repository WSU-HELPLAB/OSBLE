using System;
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
    public class DiscussionPostsTableDecorator : TableDecorator
    {
        List<DiscussionPost> AllUserPosts { get; set; }

        public DiscussionPostsTableDecorator(ITableBuilder builder, List<DiscussionPost> allUserPosts)
            : base(builder)
        {
            AllUserPosts = allUserPosts;
        }


        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam discussionTeam)
        {
            dynamic data = Builder.BuildTableForTeam(discussionTeam);
            data.Posts = new DynamicDictionary();
            
            TeamMember member = discussionTeam.Team.TeamMembers.FirstOrDefault();

            data.Posts.PostCount = 0;
            if (member != null)
            {
                data.Posts.PostCount = (from a in AllUserPosts
                                  where a.CourseUserID == member.CourseUserID &&
                                  a.DiscussionTeamID == (discussionTeam as DiscussionTeam).ID &&
                                  !a.IsReply
                                  select a).Count();
            }

            data.Posts.DiscussionTeamID = (discussionTeam as DiscussionTeam).ID;
            data.Posts.CourseUserID = member.CourseUserID;
            data.Posts.AssignmentID = discussionTeam.AssignmentID;
            return data;
        }
    }
}