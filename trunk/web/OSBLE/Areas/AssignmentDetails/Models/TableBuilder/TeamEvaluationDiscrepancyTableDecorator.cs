using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class TeamEvaluationDiscrepancyTableDecorator : TableDecorator
    {
        public List<TeamMemberEvaluation> TeamEvaluations { get; set; }
        public TeamEvaluationDiscrepancyTableDecorator(ITableBuilder builder, List<TeamMemberEvaluation> evaluations)
            : base(builder)
        {
            TeamEvaluations = evaluations;
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            //Gathering all evals for all the team members and putting their average into studentDict
            Dictionary<int, double> studentDict = new Dictionary<int, double>();
            double discrepancy = 0.0;
            foreach (TeamMember tm in assignmentTeam.Team.TeamMembers)
            {
                int denom = (from t in TeamEvaluations
                             where t.RecipientID == tm.CourseUserID
                             select t).Count();
                if (denom > 0)
                {
                    double avg = (from t in TeamEvaluations
                                  where t.RecipientID == tm.CourseUserID
                                  select t.Points).Sum() / denom;
                    studentDict.Add(tm.CourseUserID, avg);
                }
            }

            //going through all the averaged values and extracting the one with the largest difference from 100.
            foreach (KeyValuePair<int, double> pair in studentDict)
            {
                if (Math.Abs(pair.Value - 100) > discrepancy)
                {
                    discrepancy = Math.Abs(pair.Value - 100);
                }
            }

            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.TeamEvaluationDiscrepancy = discrepancy;
            return data;
        }
    }
}