using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class StudentGradeDecorator : HeaderDecorator
    {
        public StudentGradeDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            dynamic header = Builder.BuildHeader(assignment);

            // get the name of the team of the current user
            //OSBLE.Controllers.AssignmentController.GetAssignmentTeam(assignment, );
            

            return header;
        }


    }
}
