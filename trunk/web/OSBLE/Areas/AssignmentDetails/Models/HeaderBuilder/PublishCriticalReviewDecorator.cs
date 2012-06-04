using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class PublishCriticalReviewDecorator : HeaderDecorator
    {
        public PublishCriticalReviewDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.PublishCR = new DynamicDictionary();

            header.PublishCR.assignmentID = assignment.ID;

            return header;
        }
    }
}