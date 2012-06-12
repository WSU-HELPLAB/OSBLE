using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Controllers;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class CriticalReviewStudentDownloadDecorator : HeaderDecorator
    {
        public CourseUser Student { get; set; }

        public CriticalReviewStudentDownloadDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.CRdownload = new DynamicDictionary();

            header.CRdownload.hasPublished = assignment.IsCriticalReviewPublished;
            header.CRdownload.publishedDate = assignment.CriticalReviewPublishDate;

            //get student's team

                        
            AssignmentTeam assignmentTeam = null;
            assignmentTeam = OSBLEController.GetAssignmentTeam(assignment, Student);
           
            header.CRdownload.hasRecievedReview = false;

            if(assignmentTeam != null)
            {
                //get list of all teams reviewing student
                List<AssignmentTeam> reviewersOfStudent = (from rt in assignment.ReviewTeams
                                                       join at in assignment.AssignmentTeams
                                                       on rt.ReviewTeamID equals at.TeamID
                                                       where rt.AuthorTeamID == assignmentTeam.TeamID
                                                       select at).ToList();
                //check each team for a submission
                foreach (AssignmentTeam at in reviewersOfStudent)
                {
                    if(at.GetSubmissionTime() != null)
                    {
                        header.CRdownload.hasRecievedReview = true;
                        break;
                    }
                }
            }
            header.CRdownload.student = Student;
            header.CRdownload.assignmentID = assignment.ID;

            return header;
        }
    }
}