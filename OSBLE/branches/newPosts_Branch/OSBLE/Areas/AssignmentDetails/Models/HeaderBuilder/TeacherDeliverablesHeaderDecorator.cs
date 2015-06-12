using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class TeacherDeliverablesHeaderDecorator : HeaderDecorator
    {
        public TeacherDeliverablesHeaderDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.Deliverables = new DynamicDictionary();
            header.Assignment = assignment;
            header.Deliverables.AllDeliverables = assignment.Deliverables;
            
            
            //set header information
            header.Deliverables.SubmissionCount = assignment.GetSubmissionCount();
            header.Deliverables.NumberOfTeams = assignment.AssignmentTeams.Count;

            return header;
        }
    }
}