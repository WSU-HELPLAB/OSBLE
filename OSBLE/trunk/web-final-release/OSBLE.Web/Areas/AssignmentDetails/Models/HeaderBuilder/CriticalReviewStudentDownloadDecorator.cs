using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Controllers;
using OSBLE.Models;
using OSBLE.Models.Annotate;

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
            header.Assignment = assignment;
            header.CRdownload = new DynamicDictionary();

            header.CRdownload.hasPublished = assignment.IsCriticalReviewPublished;
            header.CRdownload.publishedDate = assignment.CriticalReviewPublishDate;
            header.CRdownload.hasRubricToView = false;

            //get student's team

            AssignmentTeam assignmentTeam = null;
            assignmentTeam = OSBLEController.GetAssignmentTeam(assignment.PreceedingAssignment, Student);
            header.CRdownload.teamID = assignmentTeam.TeamID;
            header.CRdownload.hasRecievedReview = false;

            //PDF reviews don't get sent to the file system (they get sent to annotate)
            //so we can't check the file system for review items.
            //yc: but we can check to see if the file has been submitted into annotate
            //dbo.annotatedocumentreferences has a field osbledocumentcode, and we have the date it was "uploaded"
            //it follows the format:#-#-#-filename.PDF == courseID-PreviousAssignmentid-teamthatisreviewingreviewing-filename.PDF
            //we also have dbo.reviewteams that show if some one has reviewed your assignment
            Assignment previousAssignment = assignment.PreceedingAssignment;
            //yc: locate all reivew teams
            ReviewTeam reviewers = null;
            bool foundAnnotateEntry = false;
            using (OSBLEContext db = new OSBLEContext())
            {
                if (assignmentTeam != null)
                    reviewers = (from rte in db.ReviewTeams
                                 where rte.AuthorTeamID == assignmentTeam.TeamID
                                 select rte).FirstOrDefault();

                if (reviewers != null)
                {
                    //if a review team exists, determin if the annoation exisits
                    string lookup = assignment.CourseID.ToString() + "-" + previousAssignment.ID.ToString() + "-" +
                        assignmentTeam.TeamID.ToString() + "-";
                    AnnotateDocumentReference d = (from adr in db.AnnotateDocumentReferences
                                                   where adr.OsbleDocumentCode.Contains(lookup)
                                                   select adr).FirstOrDefault();
                    if (d != null && (assignment.DueDate < DateTime.Now || assignment.IsCriticalReviewPublished))
                    {
                        foundAnnotateEntry = true;
                    }
                }

            }
            if (foundAnnotateEntry)//(
                //previousAssignment.HasDeliverables
                //&& previousAssignment.Deliverables[0].DeliverableType == DeliverableType.PDF
                //&& previousAssignment.Deliverables.Count == 1
                //)
            {
                header.CRdownload.hasRecievedReview = true;
            }
            else
            {
                if (assignmentTeam != null)
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
                        //if(at.GetSubmissionTime() != null)
                        if (FileSystem.GetSubmissionTime(at, assignmentTeam.Team) != null)
                        {
                            header.CRdownload.hasRecievedReview = true;
                            break;
                        }
                    }
                }

                //check if there is at one student rubric that has been filled out for the current user
                if (assignment.HasStudentRubric)
                {
                    using (OSBLEContext db = new OSBLEContext())
                    {
                        header.CRdownload.hasRubricToView = (from e in db.RubricEvaluations
                                                             where e.AssignmentID == assignment.ID &&
                                                             e.RecipientID == assignmentTeam.TeamID &&
                                                             e.Evaluator.AbstractRoleID == (int)CourseRole.CourseRoles.Student &&
                                                             e.DatePublished != null
                                                             select e.ID).Count() > 0;

                    }
                }
            }
            header.CRdownload.student = Student;
            header.CRdownload.assignmentID = assignment.ID;

            return header;
        }
    }
}
