using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Models.Assignments
{
    public class StudioAssignment : AbstractAssignment
    {
        public StudioAssignment()
        {
            AssignmentActivities = new List<AssignmentActivity>();
        }

        [Required]
        public virtual ICollection<AssignmentActivity> AssignmentActivities { get; set; }
    }
}