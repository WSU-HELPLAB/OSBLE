using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models;
using OSBLE.Controllers;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class RubricGradeDecorator : HeaderDecorator
    {

        public CourseUser Student { get; set; }

        public RubricGradeDecorator(IHeaderBuilder builder, CourseUser student)
            : base(builder)
        {
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {

            dynamic header = Builder.BuildHeader(assignment);
            header.AssignmentDetails = new DynamicDictionary();


            RubricEvaluation rubricEvaluation = null;


            //Getting the assignment team for Student, and if its non-null then we take that team ID and find the RubricEvaluation
            //that they were the recipient of. 
            AssignmentTeam at = OSBLEController.GetAssignmentTeam(assignment, Student);
            int teamId = 0;
            if(at != null)
            {
                teamId = at.TeamID;

                using (OSBLEContext db = new OSBLEContext())
                {
                    //Only want to look at evaluations where Evaluator.AbstractRole.CanGrade is true, otherwise
                    //the rubric evaluation is a  student rubric (not interested in them here)
                    rubricEvaluation = (from re in db.RubricEvaluations
                                        where re.AssignmentID == assignment.ID &&
                                        re.Evaluator.AbstractRole.CanGrade &&
                                        re.RecipientID == teamId
                                        select re).FirstOrDefault();
                }
            }

            //If the Rubric has been evaluated and is published, calculate the rubric grade % to display to the student
            if (rubricEvaluation != null && rubricEvaluation.IsPublished)
            {
                header.AssignmentDetails.hasGrade = true;
                header.AssignmentDetails.assignmentID = assignment.ID;
                header.AssignmentDetails.cuID = Student.ID;

                header.AssignmentDetails.DisplayValue = RubricEvaluation.GetGradeAsPercent(rubricEvaluation.ID);
            }
            else
            {
                header.AssignmentDetails.DisplayValue = "Not Graded";
                header.AssignmentDetails.hasGrade = false;
            }   
            return header;
        }
    }
}
