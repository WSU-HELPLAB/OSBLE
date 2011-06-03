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
        [Display(Name = "Assignment Name")]
        public string Name { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public virtual Category Category { get; set; }

        [NotMapped]
        public DateTime? StartDate { get { return startDate; } }
        private DateTime? startDate;

        [NotMapped]
        public DateTime? EndDate { get { return endDate; } }
        private DateTime? endDate;

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

            // Find start of assignment based on first activity that is not a StopActivity
            AssignmentActivity firstActivity = AssignmentActivities.Where(aa => !(aa is StopActivity)).OrderBy(aa => aa.ReleaseDate).FirstOrDefault();
            if (firstActivity != null)
            {
                startDate = firstActivity.ReleaseDate;
            }
            else
            {
                startDate = null;
            }

            // Find last activity of assignment. Sets end date if stop activity is the last activity. 
            // If not a stop activity at the end, return null.
            AssignmentActivity lastActivity = AssignmentActivities.OrderByDescending(aa => aa.ReleaseDate).FirstOrDefault();
            if (firstActivity != null && firstActivity is StopActivity)
            {
                endDate = firstActivity.ReleaseDate;
            }
            else
            {
                endDate = null;
            }


        }
    }
}