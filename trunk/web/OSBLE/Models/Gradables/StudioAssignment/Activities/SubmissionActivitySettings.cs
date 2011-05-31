using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Gradables.StudioAssignment
{
    public class SubmissionActivitySettings : AssignmentActivity
    {
        public SubmissionActivitySettings()
            : base()
        {
            isGradeable = true;
        }

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Deliverables")]
        public virtual ICollection<Deliverable> Deliverables { get; set; }

        [Required]
        [Display(Name = "Is Team")]
        public bool isTeam { get; set; }

        [Required]
        [Display(Name = "Can Instructor Do A Line By Line Review?")]
        public bool InstructorCanReview { get; set; }

        //NEED RUBRIC
    }
}