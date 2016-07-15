using System.Web;
using System;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Annotate;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.IO;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Controllers;
using OSBLE.Models;
using OSBLE.Utility;
using FileCacheHelper = OSBLEPlus.Logic.Utility.FileCacheHelper;

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
            header.IsAnnotatable = new bool();
            header.DeliverableType = assignment.Deliverables.FirstOrDefault().Type;

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

           
            //handle link for viewing annotated documents
            RubricEvaluation rubricEvaluation = null;

            //Getting the assignment team for Student, and if its non-null then we take that team ID and find the RubricEvaluation
            //that they were the recipient of. 
            AssignmentTeam ateam = OSBLEController.GetAssignmentTeam(assignment, Student);
            int teamId = 0;
            if (ateam != null)
            {
                teamId = ateam.TeamID;
                header.Submission.authorTeamID = teamId;

                using (OSBLEContext db = new OSBLEContext())
                {
                    //Only want to look at evaluations where Evaluator.AbstractRole.CanGrade is true, otherwise
                    //the rubric evaluation is a  student rubric (not interested in them here)
                    rubricEvaluation = (from re in db.RubricEvaluations
                                        where re.AssignmentID == assignment.ID &&
                                        re.Evaluator.AbstractRole.CanGrade &&
                                        re.RecipientID == teamId
                                        select re).FirstOrDefault();
                    //if annotatable
                    if (assignment.IsAnnotatable)
                    {
                        //yc: determine if there has been an annotation for this document
                        //there will be an issue with linking from a critical review untill further discussion on how to handle this
                        //when an instructor grades this, the reviewerid of the annoation is the teamid 
                        string lookup = assignment.CourseID.ToString() + "-" + assignment.ID.ToString() + "-" +
                        teamId.ToString() + "-";
                        AnnotateDocumentReference d = (from adr in db.AnnotateDocumentReferences
                                                       where adr.OsbleDocumentCode.Contains(lookup)
                                                       select adr).FirstOrDefault();
                        if (d != null && assignment.DueDate < DateTime.UtcNow)
                        {
                            header.IsAnnotatable = true;
                        }
                        else
                            header.IsAnnotatable = false;
                    }
                    else
                        header.IsAnnotatable = false;

                }
            }

            //If the Rubric has been evaluated and is published, calculate the rubric grade % to display to the student
            if (rubricEvaluation != null && rubricEvaluation.IsPublished)
            {
                header.IsPublished = true;                
            }
            else
            {
                header.IsPublished = false;                
            }  



            return header;
        }

    }
}
