using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using OSBLE.Models.Assignments.Activities;
using System;

namespace OSBLE.Models.Assignments
{
    public class StudioAssignment : AbstractAssignment
    {

        public virtual ICollection<Deliverable> Deliverables { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        public StudioAssignment()
            : base()
        {
            if (Deliverables == null)
            {
                Deliverables = new List<Deliverable>();
            }
        }

    }
}