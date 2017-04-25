using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models;


namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class RubricGradingProgressDecorator : HeaderDecorator
    {
        /// <summary>
        /// This method builds the row in the AssignmentDetails header related to showing instructors
        /// the grading progress of rubrics. I.e:
        /// "X of Y Published"
        /// "Z saved as Draft [Publish All]"
        /// </summary>
        /// <param name="builder"></param>
        public RubricGradingProgressDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            //Here we want to display "X of Y published" and "Z saved as draft [Publish All]"
            //We only want to display this information for rubric assignments.
            dynamic header = Builder.BuildHeader(assignment);
            header.GradingProgress = new DynamicDictionary();

            //get number of items saved as draft
            int draftCount = assignment.GetSavedAsDraftCount();

            string draftString = "";
            if (draftCount == 1)
            {
                draftString = draftCount.ToString() + " rubric saved as draft";
            }
            else if (draftCount > 1)
            {
                draftString = draftCount.ToString() + " rubrics saved as draft";
            }

            //set header information
            header.GradingProgress.showDraftString = draftCount > 0;
            header.GradingProgress.publishedString = assignment.GetPublishedCount() + " of " + assignment.AssignmentTeams.Count + " rubrics have been published";
            header.GradingProgress.draftString = draftString;
            header.GradingProgress.AssignmentID = assignment.ID;

            return header;
        }

    }
}
