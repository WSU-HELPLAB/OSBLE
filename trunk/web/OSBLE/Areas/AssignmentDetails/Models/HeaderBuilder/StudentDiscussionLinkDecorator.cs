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
    public class StudentDiscussionLinkDecorator : HeaderDecorator
    {
        public CourseUser Student { get; set; }

        public StudentDiscussionLinkDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.StudentDiscussion = new DynamicDictionary();

            List<DiscussionTeam> usersTeams = new List<DiscussionTeam>();

            //build a list of all discussionTeams that the current student is on
            foreach(DiscussionTeam dt in assignment.DiscussionTeams)
            {
                foreach (TeamMember tm in dt.Team.TeamMembers)
                {
                    if (tm.CourseUserID == Student.ID)
                    {
                        usersTeams.Add(dt);
                        break;
                    }
                }
            }

            header.StudentDiscussion.DiscussionTeams = usersTeams;
            header.StudentDiscussion.assignment = assignment;
            return header;
        }
    }
}
