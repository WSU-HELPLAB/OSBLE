using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder
{
    public class PreviousAssignmentDecorator : HeaderDecorator
    {
        public PreviousAssignmentDecorator(IHeaderBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildHeader(Assignment assignment)
        {
            //assignment.PreceedingAssignment
            dynamic header = Builder.BuildHeader(assignment);
            header.PreviousAssignment = new DynamicDictionary();

            header.PreviousAssignment.ID = assignment.PrecededingAssignmentID;
            header.PreviousAssignment.name = assignment.PreceedingAssignment.AssignmentName;

            return header;
        }
    }
}