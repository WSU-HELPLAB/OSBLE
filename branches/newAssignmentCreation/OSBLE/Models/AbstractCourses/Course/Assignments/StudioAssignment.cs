using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Models.Assignments
{
    public class StudioAssignment : AbstractAssignment
    {
        public virtual ICollection<dynamic> Deliverables { get; set; }

        public StudioAssignment()
            : base()
        {
            PointsPossible = 100;

            if (Deliverables == null)
            {
                Deliverables = new List<dynamic>();
            }
        }
    }
}