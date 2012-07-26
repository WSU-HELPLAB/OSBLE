using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses.Rubrics;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class RubricTableDecorator : TableDecorator
    {
        public List<RubricEvaluation> RubricEvaluations { get; set; }

        public RubricTableDecorator(ITableBuilder builder, List<RubricEvaluation> evaluations)
            : base(builder)
        {
            RubricEvaluations = evaluations;
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

            RubricEvaluation rubricEvaluation = RubricEvaluations.Where(re => re.RecipientID == assignmentTeam.TeamID
                                                                        && re.Evaluator.AbstractRole.CanGrade).FirstOrDefault();
            data.Grade.LinkText = "Not Graded";
            if (rubricEvaluation != null)
            {
                //A rubric exists, so if it is not published, it's saved as draft. If it is 
                //published, the rubric grade should be displayed.
                if (rubricEvaluation.IsPublished == false)
                {
                    data.Grade.LinkText = "Saved as Draft";
                }
                else
                {
                    data.Grade.LinkText = rubricEvaluation.GetGradeAsPercent();
                }
            }

            return data;
        }
    }
}