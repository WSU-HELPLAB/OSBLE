using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public class SubmissionActivity : StudioActivity
    {
        public SubmissionActivity()
            : base()
        {
        }

        [Required]
        [Display(Name = "Can Instructor Do A Line By Line Review?")]
        public bool InstructorCanReview { get; set; }

        //NEED RUBRIC
    }
}