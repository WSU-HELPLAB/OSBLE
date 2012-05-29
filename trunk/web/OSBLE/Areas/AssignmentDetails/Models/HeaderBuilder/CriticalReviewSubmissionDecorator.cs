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
               
                // get submission times for critical review submission
                List<DateTime?> authorSubmissionTimes = new List<DateTime?>();
                foreach(ReviewTeam reviewTeam in authorTeams)
                {
                    submissionTimes.Add(FileSystem.GetSubmissionTime(assignmentTeam, reviewTeam.AuthorTeam));

                    AssignmentTeam assignTeam = (from at in assignment.PreceedingAssignment.AssignmentTeams
                                                 where at.TeamID == reviewTeam.AuthorTeamID
                                                 select at).FirstOrDefault();

                    authorSubmissionTimes.Add(assignTeam.GetSubmissionTime());
                }

                // get submission times for original assignment submission (in order to check if they have submitted)
                // need to loop over each AssignmentTeam from the original assignment to get submission time
                

                //pass submission times to view
                header.CRSubmission.submissionTimes = submissionTimes;
                header.CRSubmission.authorSubmissionTimes = authorSubmissionTimes;

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
