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
    public class TeamEvalGradingProgressDecorator : HeaderDecorator
    {
        public TeamEvalGradingProgressDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.GradingProgress = new DynamicDictionary();

            //get number of items published
            int publishedCount = assignment.Scores.Where(s => s.Points >= 0).Count();
            int totalAssignments = assignment.AssignmentTeams.Count();

            //set header information
            header.GradingProgress.publishedCount = publishedCount;
            header.GradingProgress.totalAssignments = totalAssignments;
            header.GradingProgress.AssignmentID = assignment.ID;

            return header;
        }
    }
}