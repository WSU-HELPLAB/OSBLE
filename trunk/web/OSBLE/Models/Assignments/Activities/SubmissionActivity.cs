using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public class SubmissionActivity : AssignmentActivity, IHasDeliverables
    {
        public SubmissionActivity()
            : base()
        {
            Deliverables = new List<Deliverable>();
        }

        [Display(Name = "Deliverables")]
        public virtual ICollection<Deliverable> Deliverables { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Is Team")]
        public bool isTeam { get; set; }

        [Required]
        [Display(Name = "Can Instructor Do A Line By Line Review?")]
        public bool InstructorCanReview { get; set; }

        //NEED RUBRIC
    }
}