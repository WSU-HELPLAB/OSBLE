using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public interface ITableBuilder
    {
        DynamicDictionary BuildTableForTeam(Team team);
    }
}