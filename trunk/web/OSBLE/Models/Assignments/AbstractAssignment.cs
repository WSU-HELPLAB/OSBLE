using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities;

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


        [NotMapped]
        public DateTime? StartDate { get { return _startDate; } }
        private DateTime? _startDate;

        [NotMapped]
        public DateTime? EndDate { get { return _endDate; } }
        private DateTime? _endDate;

        [Required]
        public virtual ICollection<AssignmentActivity> AssignmentActivities { get; set; }

        public AbstractAssignment() {
            // Create Activities Collection if none exists
            if (AssignmentActivities == null)
            {
                AssignmentActivities = new List<AssignmentActivity>();
            }

            // Find start of assignment based on first activity that is not a StopActivity
            AssignmentActivity firstActivity = AssignmentActivities.Where(aa => !(aa is StopActivity)).OrderBy(aa => aa.ReleaseDate).FirstOrDefault();
            if (firstActivity != null)
            {
                _startDate = firstActivity.ReleaseDate;
            }
            else
            {
                _startDate = null;
            }


        }
    }
}