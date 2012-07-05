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

            header.PublishCR.PublishButtonDisplayValue = "Publish All Reviews";
            header.PublishCR.PublishStatus = "Not Published";
            if (assignment.IsCriticalReviewPublished)
            {
                if (assignment.CriticalReviewPublishDate != null)
                {
                    header.PublishCR.PublishStatus = "Published " + assignment.CriticalReviewPublishDate.ToString();
                    header.PublishCR.PublishButtonDisplayValue = "Republish All Reviews";
                }
            }
            header.PublishCR.assignmentID = assignment.ID;

            return header;
        }
    }
}