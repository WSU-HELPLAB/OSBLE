using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;
using OSBLE.Resources;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public abstract class TableDecorator : ITableBuilder
    {
        protected ITableBuilder Builder { get; set; }

        public TableDecorator(ITableBuilder builder)
        {
            Builder = builder;
        }

        public abstract DynamicDictionary BuildTableForTeam(Team team);
    }
}