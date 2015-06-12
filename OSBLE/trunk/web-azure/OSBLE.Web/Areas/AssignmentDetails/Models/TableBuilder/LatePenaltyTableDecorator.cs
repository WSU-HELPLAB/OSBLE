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
            //discussionTeam is a put together DiscussionTeam that has a new non-db saved team for its team
                //This new team has only 1 team members (the individual we want to create a row for). 
                //but, the TeamID is the real team ID used in the database. This is done in DiscussionAssignmentIndex.cshtml
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);

            data.LatePenaltyPercent = OSBLEController.GetLatePenaltyAsString(assignmentTeam);
            return data;
        }
    }
}