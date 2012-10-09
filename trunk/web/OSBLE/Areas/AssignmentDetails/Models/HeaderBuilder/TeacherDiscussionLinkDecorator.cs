using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class TeacherDiscussionLinkDecorator : HeaderDecorator
    {
        CourseUser Client;
        public TeacherDiscussionLinkDecorator(IHeaderBuilder builder, CourseUser client)
            : base(builder)
        {
            Client = client;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.ID = assignment.ID;
            header.DiscussionTeamID = assignment.DiscussionTeams.FirstOrDefault().ID;
            header.NewPosts = assignment.DiscussionTeams.FirstOrDefault().GetNewPostsCount(Client.ID);
            return header;
        }
    }
}
