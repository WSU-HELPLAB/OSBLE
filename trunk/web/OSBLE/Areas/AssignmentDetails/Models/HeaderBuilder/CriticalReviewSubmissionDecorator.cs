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


            // get the assignment team ( team doing the review )
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

            header.CRSubmission.hasSubmitted = false;
            

            if (assignmentTeam != null)
            {
                List<ReviewTeam> authorTeams = new List<ReviewTeam>();
                authorTeams = (from rt in assignment.ReviewTeams
                               where rt.ReviewTeamID == assignmentTeam.TeamID
                               select rt).ToList();
                header.CRSubmission.authorTeams = authorTeams;
                header.CRSubmission.assignmentId = assignment.ID;

                List<DateTime?> submissionTimes = new List<DateTime?>();
               
                // get submission time
                foreach(ReviewTeam reviewTeam in authorTeams)
                {
                    submissionTimes.Add(FileSystem.GetSubmissionTime(assignmentTeam, reviewTeam.AuthorTeam)); 
                }

                header.CRSubmission.submissionTimes = submissionTimes;
                if (assignment.HasTeams)
                {
                    header.CRSubmission.hasTeams = true;
                }
                else
                {
                    header.CRSubmission.hasTeams = false;
                }
            }
            
            return header;
        }
    }
}
