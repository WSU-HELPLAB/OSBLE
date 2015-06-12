using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class StudentTeamEvalSubmissionDecorator : HeaderDecorator
    {
        public List<TeamEvaluation> TeamEvals { get; set; }
        public CourseUser Student { get; set; }
        public StudentTeamEvalSubmissionDecorator(IHeaderBuilder builder, 
            List<TeamEvaluation> teamEvals, 
            CourseUser student)
            : base(builder)
        {
            TeamEvals = teamEvals;
            Student = student;
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);
            header.TeamEval = new DynamicDictionary();

            if ((from te in TeamEvals
                 where te.EvaluatorID == Student.ID
                 select te).Count() >= 1)
            {
                header.TeamEval.displayValue = "View/Edit Team Evaluation";
            }
            else
            {
                header.TeamEval.displayValue = "Submit Team Evaluation";
            }

            header.TeamEval.assignmentId = assignment.ID;

            

            return header;
        }
    }
}
