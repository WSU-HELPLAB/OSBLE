using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class ABETProficiencyDecorator : TableDecorator
    {
        public ABETProficiencyDecorator(ITableBuilder builder)
            : base(builder)
        {
        }
        
        public override Resources.DynamicDictionary BuildTableForTeam(OSBLE.Models.Assignments.IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.AssignmentTeam = assignmentTeam;
            return data;
        }
    }
}