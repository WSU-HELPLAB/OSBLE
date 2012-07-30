using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Controllers;

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

            data.LatePenaltyPercent = OSBLEController.GetLatePenaltyAsString(assignmentTeam);
            return data;
        }
    }
}