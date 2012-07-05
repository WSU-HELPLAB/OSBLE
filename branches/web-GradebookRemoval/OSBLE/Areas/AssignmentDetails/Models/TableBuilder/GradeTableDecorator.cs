using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses.Rubrics;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class GradeTableDecorator : TableDecorator
    {
        public List<RubricEvaluation> RubricEvaluations { get; set; }

        public GradeTableDecorator(ITableBuilder builder, List<RubricEvaluation> evaluations)
            : base(builder)
        {
            RubricEvaluations = evaluations;
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            Assignment assignment = assignmentTeam.Assignment;
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.AssignmentTeam = assignmentTeam;

            data.Grade = new DynamicDictionary();
            Score score = data.Score;

            //don't pull if we already have it from somewhere else
            if (score == null)
            {
                score = assignmentTeam.Assignment.Scores.Where(s => s.TeamID == assignmentTeam.TeamID).FirstOrDefault();
                data.Score = score;
            }
            
            //set default values (send to gradebook)
            data.Grade.LinkText = "Not Graded";
            data.Grade.Action = "Tab";
            data.Grade.Controller = "Gradebook";
            data.Grade.ActionValues = new { categoryId = assignment.CategoryID, area = "" };

            if (assignment.HasRubric)
            {
                //switch link from gradebook to rubric
                data.Grade.Action = "Index";
                data.Grade.Controller = "Rubric";
                data.Grade.ActionValues = new
                {
                    assignmentId = assignment.ID,
                    cuId = assignmentTeam.Team.TeamMembers.FirstOrDefault().CourseUserID,
                    area = ""
                };

                RubricEvaluation rubricEvaluation = RubricEvaluations.Where(re => re.RecipientID == assignmentTeam.TeamID).FirstOrDefault();
                if (rubricEvaluation != null)
                {
                    if (rubricEvaluation.IsPublished == false)
                    {
                        data.Grade.LinkText = "Saved as Draft";
                    }
                }
            }

            if (score != null)
            {
                data.Grade.LinkText = score.getGradeAsPercent(assignment.PointsPossible);
            }
            return data;
        }
    }
}