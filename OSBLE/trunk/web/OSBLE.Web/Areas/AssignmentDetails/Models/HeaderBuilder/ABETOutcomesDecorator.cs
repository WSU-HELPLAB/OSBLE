using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class ABETOutcomesDecorator : HeaderDecorator
    {
        public ABETOutcomesDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            
            // Build a list of strings for ABET outcomes
            List<string> outcomes = new List<string>();
            foreach (AbetAssignmentOutcome o in assignment.ABETOutcomes)
            {
                outcomes.Add(o.Outcome);
            }
            header.ABETOutcomes = outcomes;

            return header;
        }
    }
}
