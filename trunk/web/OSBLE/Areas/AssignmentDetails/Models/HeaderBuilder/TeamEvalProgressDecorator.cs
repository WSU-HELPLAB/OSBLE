using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models;


namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class TeamEvalProgressDecorator : HeaderDecorator
    {
        public List<TeamEvaluation> TeamEvaluations;
        public TeamEvalProgressDecorator(IHeaderBuilder builder, List<TeamEvaluation> teamEvaluations)
            : base(builder)
        {
            TeamEvaluations = teamEvaluations;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.GradingProgress = new DynamicDictionary();

            //Normally, a student completing an evaluation for a team of 3 people yields 3 team evaluations. We want to mask this
            //from the user and show it as: 1 student completed an evaluation for his team, so 1 teamEvaluation has been completed. 
            //So, the number of completedEvaluations is actually the number of evaluations that have distinct evaluators
            //and the number of total evaluations is the number of students particpating in the assignment.

            //get number of team evaluations completed by unique evaluators. 
            //I.e. if John morgan completes evaluations for 3 others, only count his 3 evaluations as 1 because they all have the same evaluator.
            int completedEvals = (from te in TeamEvaluations
                                  select new { id = te.EvaluatorID }).Distinct().Count();

            //get the number of all team members for this assignment. 
            int totalEvals = assignment.GetTotalTeamEvaluationCount();

            //set header information
            header.GradingProgress.completedEvals = completedEvals;
            header.GradingProgress.totalEvals = totalEvals;

            return header;
        }
    }
}