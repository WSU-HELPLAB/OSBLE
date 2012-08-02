using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments
{
    public interface IAssignmentTeam : IModelBuilderExtender
    {
        int AssignmentID { get; set; }
        Assignment Assignment { get; set; }

        int TeamID { get; set; }

        Team Team { get; set; }
    }
}