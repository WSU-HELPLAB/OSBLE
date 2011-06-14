using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;

namespace OSBLE.Models.Assignments
{
    public abstract class AbstractAssignment
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public virtual Category Category { get; set; }

        [Required]
        public virtual ICollection<AssignmentActivity> AssignmentActivities { get; set; }

        /// <summary>
        /// Grading points possible relative to sibling assignments in the parent category.
        /// </summary>
        public int PointsPossible { get; set; }

        /// <summary>
        /// Used for visual ordering of assignments in the gradebook.  Defaults to a value of 0.
        /// </summary>
        public int ColumnOrder { get; set; }

        public AbstractAssignment() {
            
            ColumnOrder = 0;

            // Create Activities Collection if none exists
            if (AssignmentActivities == null)
            {
                AssignmentActivities = new List<AssignmentActivity>();
            }

        }
    }
}