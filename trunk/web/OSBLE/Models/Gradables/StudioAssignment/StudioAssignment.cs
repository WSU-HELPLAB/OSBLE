using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Gradables.StudioAssignment
{
    public class StudioAssignment
    {
        public virtual ICollection<AssignmentActivity> AssignmentActivities;
    }
}