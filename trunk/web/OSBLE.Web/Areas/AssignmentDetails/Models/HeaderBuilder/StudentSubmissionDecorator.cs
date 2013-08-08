using System.Web;
using System;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using OSBLE.Utility;
using OSBLE.Models.FileSystem;
using System.IO;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class StudentSubmissionDecorator : HeaderDecorator
    {
        public CourseUser Student { get; set; }

        public StudentSubmissionDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.Submission = new DynamicDictionary();

            DateTime? submissionTime = null;

            //get id of current student's team
            List<TeamMember> allMembers = assignment.AssignmentTeams.SelectMany(at => at.Team.TeamMembers).ToList();
            TeamMember member = allMembers.Where(m => m.CourseUserID == Student.ID).FirstOrDefault();

            header.Submission.allowSubmit = true;

            //get submission time:
            foreach (AssignmentTeam team in assignment.AssignmentTeams)
            {
                //if the team matches with the student
                if (team.TeamID == member.TeamID)
                {
                    OSBLEDirectory submission = Directories.GetAssignmentSubmission(Student.AbstractCourseID, assignment.ID, team.TeamID);
                    if (submission.GetSubmissionTime() != null)
                    {
                        submissionTime = submission.GetSubmissionTime();
                        break;
                    }
                }
            }

            if (submissionTime == null)
            {
                header.Submission.hasSubmitted = false;
            }
            else
            {
                header.Submission.hasSubmitted = true;
                header.Submission.SubmissionTime = submissionTime.Value.ToString();
            }

            header.Submission.assignmentID = assignment.ID;

            FileCache Cache = FileCacheHelper.GetCacheInstance(OsbleAuthentication.CurrentUser);

            //Same functionality as in the other controller. 
            //did the user just submit something?  If so, set up view to notify user
            if (Cache["SubmissionReceived"] != null && Convert.ToBoolean(Cache["SubmissionReceived"]) == true)
            {
                header.Submission.SubmissionReceived = true;
                Cache["SubmissionReceived"] = false;
            }
            else
            {
                header.Submission.SubmissionReceived = false;
                Cache["SubmissionReceived"] = false;
            }


            return header;
        }

    }
}
