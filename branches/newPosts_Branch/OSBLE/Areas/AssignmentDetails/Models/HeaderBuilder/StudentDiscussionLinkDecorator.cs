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


        /// <summary>
        /// This Decorater is used to link to classwide discussions for student's. Discussion assignments with discussion teams link 
        /// to the discussions with the DiscussionTeamMEmberDecorator.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="student"></param>
        public StudentDiscussionLinkDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.StudentDiscussion = new DynamicDictionary();

            //build a list of all discussionTeams that the current student is on
            bool breakout = false;
            foreach(DiscussionTeam dt in assignment.DiscussionTeams)
            {
                foreach (TeamMember tm in dt.GetAllTeamMembers())
                {
                    if (tm.CourseUserID == Student.ID)
                    {
                        header.StudentDiscussion.DiscussionTeam = dt;
                        header.StudentDiscussion.NewPosts = dt.GetNewPostsCount(Student.ID);
                        breakout = true;
                        break;
                    }
                }
                if(breakout)
                    break;
            }

            return header;
        }
    }
}
