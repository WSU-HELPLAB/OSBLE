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
    public class CriticalReviewSubmissionDecorator : HeaderDecorator
    {
        public CourseUser Student { get; set; }

        public CriticalReviewSubmissionDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.CRSubmission = new DynamicDictionary();

            //Foreach author
                //get last submission time


            AssignmentTeam assignmentTeam = null;
            foreach (AssignmentTeam at in assignment.AssignmentTeams)
            {
                foreach (TeamMember tm in at.Team.TeamMembers)
                {
                    if (tm.CourseUser.UserProfileID == Student.UserProfileID )
                    {
                        assignmentTeam = at;
                    }
                }
            }

            if (assignmentTeam != null)
            {
                List<ReviewTeam> authorTeams = new List<ReviewTeam>();
                authorTeams = (from rt in assignment.ReviewTeams
                               where rt.ReviewTeamID == assignmentTeam.TeamID
                               select rt).ToList();
                header.CRSubmission.authorTeams = authorTeams;
                header.CRSubmission.assignmentId = assignment.ID;
            }
            
            return header;
        }
    }
}
