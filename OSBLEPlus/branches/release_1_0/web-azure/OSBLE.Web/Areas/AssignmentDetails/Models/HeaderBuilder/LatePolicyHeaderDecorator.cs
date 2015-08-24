using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class LatePolicyHeaderDecorator : HeaderDecorator
    {
        public LatePolicyHeaderDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.LatePolicy = new DynamicDictionary();
            

            string latePolicyMessage = null;
            if (assignment.HoursLateWindow == 0)
            {
                latePolicyMessage = "No late assignments accepted.";
            }
            else if (assignment.Type == AssignmentTypes.CriticalReviewDiscussion || assignment.Type == AssignmentTypes.DiscussionAssignment)
            {
                latePolicyMessage = String.Format("Initial posts accepted up to {0} hours late, docking {1}% per {2} hours.", assignment.HoursLateWindow, assignment.DeductionPerUnit, assignment.HoursPerDeduction);
            }
            else
            {
                latePolicyMessage = String.Format("Submissions accepted up to {0} hours late, docking {1}% per {2} hours.", assignment.HoursLateWindow, assignment.DeductionPerUnit, assignment.HoursPerDeduction);
            }


            header.LatePolicy.Message = latePolicyMessage;
            return header;
        }
    }
}