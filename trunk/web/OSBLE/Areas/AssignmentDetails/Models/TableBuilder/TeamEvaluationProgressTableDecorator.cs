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
        public TeamEvaluationProgressTableDecorator(ITableBuilder builder, List<TeamEvaluation> evaluations)
            : base(builder)
        {
            Evaluations = evaluations;
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.AssignmentTeam = assignmentTeam;
            data.TeamEvaluationProgress = new DynamicDictionary();
            data.TeamEvaluationProgress.CompletedEvaluations = Evaluations.Count;
            data.TeamEvaluationProgress.TotalEvaluations = assignmentTeam.Team.TeamMembers.Count;
            return data;
        }
    }
}