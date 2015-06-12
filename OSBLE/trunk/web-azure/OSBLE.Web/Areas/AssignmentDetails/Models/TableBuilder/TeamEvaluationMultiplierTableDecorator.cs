using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class TeamEvaluationMultiplierTableDecorator : TableDecorator
    {
        public List<TeamEvaluation> TeamEvaluations { get; set; }
        public TeamEvaluationMultiplierTableDecorator(ITableBuilder builder, List<TeamEvaluation> evaluations)
            : base(builder)
        {
            TeamEvaluations = evaluations;
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.Multiplier = new DynamicDictionary();
            
            // Note: assume assignment team size == 1 because TeamEvaluationIndex
            // splits each courseUser into their own seperate team
            double avg = -1;
            int denom = (from t in TeamEvaluations
                            where t.RecipientID == assignmentTeam.Team.TeamMembers.FirstOrDefault().CourseUserID
                            select t).Count();
            if (denom > 0)
            {
                avg = (from t in TeamEvaluations
                                where t.RecipientID == assignmentTeam.Team.TeamMembers.FirstOrDefault().CourseUserID
                                select t.Points).Sum() / denom;
            }

            if (avg == -1)
            {
                data.Multiplier.MultiplierText = "-";
            }
            else
            {
                data.Multiplier.MultiplierText = (avg /= 100).ToString();
            }
     
            return data;
        }
    }
}