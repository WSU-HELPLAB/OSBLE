using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Controllers;
using System.IO;
using System.Text;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class StudentRubricsReceivedDecorator : TableDecorator
    {
        public StudentRubricsReceivedDecorator(ITableBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.studentRubricsReceived = new DynamicDictionary();

            return data;
        }

    }
}
