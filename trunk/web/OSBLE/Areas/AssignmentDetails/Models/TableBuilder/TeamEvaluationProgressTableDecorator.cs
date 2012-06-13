using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models;
using OSBLE.Models.Courses.Rubrics;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class TeamEvaluationProgressTableDecorator : TableDecorator
    {
        public List<TeamEvaluation> Evaluations { get; set; }
        public Assignment Assignment { get; set; }
        public TeamEvaluationProgressTableDecorator(ITableBuilder builder, 
                                                    List<TeamEvaluation> evaluations, 
                                                    Assignment assignment)
            : base(builder)
        {
            Evaluations = evaluations;
            Assignment = assignment;
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);

            data.TeamEvaluationProgress = new DynamicDictionary();

            //Count number of completed evaluations done by the current team
            int completedEvaluations = (from te in Evaluations
                                        where assignmentTeam.Team.TeamMembers.Where(tm => tm.CourseUserID == te.RecipientID).Count() > 0
                                        select te).Count();


            completedEvaluations /= assignmentTeam.Team.TeamMembers.Count();

            data.TeamEvaluationProgress.CompletedEvaluations = completedEvaluations;
            data.TeamEvaluationProgress.TotalEvaluations = assignmentTeam.Team.TeamMembers.Count;
            data.TeamEvaluationProgress.AssignmentId = Assignment.ID;
            data.TeamEvaluationProgress.AssignmentTeam = assignmentTeam;
            return data;
        }
    }
}