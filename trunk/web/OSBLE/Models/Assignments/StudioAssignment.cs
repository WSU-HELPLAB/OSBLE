using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Models.Assignments
{
    public class StudioAssignment : AbstractAssignment
    {
        public virtual ICollection<Deliverable> Deliverables { get; set; }

        [Required]
        [Display(Name = "Description")]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Draft Assignment")]
        public bool IsDraft { get; set; }

        public StudioAssignment()
            : base()
        {
            PointsPossible = 100;

            if (Deliverables == null)
            {
                Deliverables = new List<Deliverable>();
            }
        }
    }
}