using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class InitialFinalDueDecorator : HeaderDecorator
    {
        public InitialFinalDueDecorator(IHeaderBuilder builder)
            : base(builder)
        {

        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.InitialPostDue = assignment.DiscussionSettings.InitialPostDueDate;
            header.DueDate = assignment.DueDate;

            return header;
        }
    }
}
