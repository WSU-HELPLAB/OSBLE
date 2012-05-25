using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Resources;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class TeacherCriticalReviewDecorator : TableDecorator
    {
        public TeacherCriticalReviewDecorator(ITableBuilder builder)
            :base(builder)
        {
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);

            //get information to download all reviews that the team did

            data.AssignmentTeam = assignmentTeam;

            return data;
        }
    }
}
