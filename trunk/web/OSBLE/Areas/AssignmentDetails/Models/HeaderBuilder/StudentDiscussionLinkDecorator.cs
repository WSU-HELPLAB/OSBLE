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

            //Finding discussion team user is on. Despite the assingment being classwide, we want
            //the user to always have the same DiscussionTeamID when posting
            bool breakout = false;
            foreach(DiscussionTeam dt in assignment.DiscussionTeams)
            {
                foreach (TeamMember tm in dt.GetAllTeamMembers())
                {
                    //Found team user is on
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

            //Moderators are not part of a team. Allow them to view the discussion with any discussionTeam
            if (header.StudentDiscussion.DiscussionTeam == null)
            {
                header.StudentDiscussion.DiscussionTeam = assignment.DiscussionTeams.FirstOrDefault();
                header.StudentDiscussion.NewPosts = 0;
            }
            

            return header;
        }
    }
}
