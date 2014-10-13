using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class RubricTableDecorator : TableDecorator
    {

        /// <summary>
        /// This function will be used to create a column for the instructor table that shows
        /// "Not Graded", "Saved as Draft", or a grade % for a rubric based assingment
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="evaluations"></param>
        public RubricTableDecorator(ITableBuilder builder)
            : base(builder)
        {
        }

        /// <summary>
        /// This function builds the column for Rubric Grades in the instructor assignment details table. Note: This only handles non-student-evaluated rubrics.
        /// </summary>
        /// <param name="assignmentTeam"></param>
        /// <returns></returns>
        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {

            Assignment assignment = assignmentTeam.Assignment;
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.Grade = new DynamicDictionary();
            data.Grade.ActionValues = new
            {
                assignmentId = assignment.ID,
                cuId = assignmentTeam.Team.TeamMembers.FirstOrDefault().CourseUserID,
                area = ""
            };

            RubricEvaluation rubricEvaluation = null;

            using (OSBLEContext db = new OSBLEContext())
            {
                rubricEvaluation = db.RubricEvaluations.Where(re => re.RecipientID == assignmentTeam.TeamID
                                                                && re.Evaluator.AbstractRole.CanGrade &&
                                                                re.AssignmentID == assignment.ID
                                                            ).FirstOrDefault();
            }
            
            data.Grade.LinkText = "Not Graded";
            if (rubricEvaluation != null)
            {
                //A rubric exists, so if it is not published, it's saved as draft. If it is 
                //published, the rubric grade should be displayed.
                if (rubricEvaluation.IsPublished == false)
                {
                    data.Grade.LinkText = "Saved as Draft (" + RubricEvaluation.GetGradeAsPercent(rubricEvaluation.ID) + ")";
                }
                else
                {
                    data.Grade.LinkText = RubricEvaluation.GetGradeAsPercent(rubricEvaluation.ID);
                }
            }

            return data;
        }
    }
}