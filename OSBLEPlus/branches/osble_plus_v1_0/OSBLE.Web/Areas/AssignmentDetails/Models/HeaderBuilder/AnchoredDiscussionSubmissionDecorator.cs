using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using System.Runtime.Caching;
using OSBLE.Controllers;
using OSBLE.Utility;
using FileCacheHelper = OSBLEPlus.Logic.Utility.FileCacheHelper;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class AnchoredDiscussionSubmissionDecorator : HeaderDecorator
    {
        public CourseUser Student { get; set; }

        public AnchoredDiscussionSubmissionDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            //TODO: remove un-needed code
            dynamic header = Builder.BuildHeader(assignment);
            header.CRSubmission = new DynamicDictionary();
            header.Assignment = assignment;
            
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
                //List<DateTime?> authorSubmissionTimes = new List<DateTime?>();
                //foreach(ReviewTeam reviewTeam in authorTeams)
                //{
                //    submissionTimes.Add(FileSystem.GetSubmissionTime(assignmentTeam, reviewTeam.AuthorTeam));

                //    AssignmentTeam assignTeam = (from at in assignment.PreceedingAssignment.AssignmentTeams
                //                                 where at.TeamID == reviewTeam.AuthorTeamID
                //                                 select at).FirstOrDefault();

                //    authorSubmissionTimes.Add(assignTeam.GetSubmissionTime());
                //}

                // get submission times for original assignment submission (in order to check if they have submitted)
                // need to loop over each AssignmentTeam from the original assignment to get submission time
                

                //pass submission times to view
                header.CRSubmission.submissionTimes = submissionTimes;
                //header.CRSubmission.authorSubmissionTimes = authorSubmissionTimes;

                if (assignment.HasTeams)
                {
                    header.CRSubmission.hasTeams = true;
                }
                else
                {
                    header.CRSubmission.hasTeams = false;
                }
            }

            FileCache Cache = FileCacheHelper.GetCacheInstance(OsbleAuthentication.CurrentUser);
            //Same functionality as in the other controller. Note: These values are set in SubmissionController/Create[POST]
            //did the user just submit something?  If so, set up view to notify user
            if (Cache["SubmissionReceived"] != null && Convert.ToBoolean(Cache["SubmissionReceived"]) == true)
            {
                header.CRSubmission.SubmissionReceived = true;
                header.CRSubmission.AuthorTeamId = (int)Cache["SubmissionForAuthorTeamID"];
                Cache["SubmissionReceived"] = false;
            }
            else
            {
                header.CRSubmission.SubmissionReceived = false;
                Cache["SubmissionReceived"] = false;
            }

            //rubric stuff:
            header.CRSubmission.hasStudentRubric = assignment.HasStudentRubric;
            header.CRSubmission.studentRubricID = assignment.StudentRubricID;          

            return header;
        }
    }
}
