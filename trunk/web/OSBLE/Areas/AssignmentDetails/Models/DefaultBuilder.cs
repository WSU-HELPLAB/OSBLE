using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentDetails.Models.HeaderBuilder;
using OSBLE.Areas.AssignmentDetails.Models.TableBuilder;

namespace OSBLE.Areas.AssignmentDetails.Models
{
    public class DefaultBuilder : IHeaderBuilder, ITableBuilder
    {
        public DynamicDictionary BuildHeader(Assignment assignment)
        {
            DynamicDictionary dict = new DynamicDictionary();
            return dict;
        }

        public DynamicDictionary BuildTableForTeam(IAssignmentTeam team)
        {
            DynamicDictionary dict = new DynamicDictionary();
            return dict;
        }
    }
}