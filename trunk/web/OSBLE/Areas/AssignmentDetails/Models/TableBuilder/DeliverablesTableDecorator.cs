using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class DeliverablesTableDecorator : TableDecorator
    {
        public DeliverablesTableDecorator(ITableBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildTableForTeam(Team team)
        {
            dynamic data = Builder.BuildTableForTeam(team);

            return data;
        }
    }
}