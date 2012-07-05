using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class LatePenaltyTableDecorator : TableDecorator
    {
        public LatePenaltyTableDecorator(ITableBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.AssignmentTeam = assignmentTeam;
            Score score = data.Score;

            //don't pull if we already have it from somewhere else
            if (score == null)
            {
                score = assignmentTeam.Assignment.Scores.Where(s => s.TeamID == assignmentTeam.TeamID).FirstOrDefault();
                data.Score = score;
            }
            return data;
        }
    }
}