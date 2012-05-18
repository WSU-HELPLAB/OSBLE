using System.Web;
using System;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using System.Collections.Generic;
using System.Linq;

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

            //get submission time:
            foreach (AssignmentTeam team in assignment.AssignmentTeams)
            {
                //if the team matches with the student
                if (team.TeamID == member.TeamID)
                {
                    submissionTime = team.GetSubmissionTime();
                    break;
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

            return header;
        }

    }
}
