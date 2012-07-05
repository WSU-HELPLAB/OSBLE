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
    public class TeacherGradingProgressDecorator : HeaderDecorator
    {
        public TeacherGradingProgressDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.GradingProgress = new DynamicDictionary();

            //get number of items graded
            int publishedCount = assignment.Scores.Where(s => s.Points >= 0).Count();
            int totalAssignments = assignment.AssignmentTeams.Count();
            int draftCount = 0;
            using (OSBLEContext db = new OSBLEContext())
            {
                draftCount = (from e in db.RubricEvaluations
                                                      where e.AssignmentID == assignment.ID &&
                                                      e.IsPublished == false
                                                      select e).Count();
            }
            //set header information
            header.GradingProgress.publishedCount = publishedCount;
            header.GradingProgress.totalAssignments = totalAssignments;
            header.GradingProgress.draftCount = draftCount;
            header.GradingProgress.AssignmentID = assignment.ID;

            

            return header;
        }

    }
}
