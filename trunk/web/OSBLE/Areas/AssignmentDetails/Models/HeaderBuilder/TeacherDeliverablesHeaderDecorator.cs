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

            //get submissions for the assignment teams
            int submissionCount = 0;
            foreach (AssignmentTeam team in assignment.AssignmentTeams)
            {
                if (team.GetSubmissionTime() != null)
                {
                    submissionCount++;
                }
            }
            
            //set header information
            header.Deliverables.SubmissionCount = submissionCount;
            header.Deliverables.NumberOfTeams = assignment.AssignmentTeams.Count;

            return header;
        }
    }
}