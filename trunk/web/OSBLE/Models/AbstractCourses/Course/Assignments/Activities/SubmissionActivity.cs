using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;

namespace OSBLE.Models.Assignments.Activities
{
    public class SubmissionActivity : StudioActivity
    {
        public SubmissionActivity()
            : base()
        {
            Teams = new List<Team>();
        }

        [Required]
        [Display(Name = "This is a team assignment")]
        public bool isTeam { get; set; }

        public ICollection<Team> Teams { get; set; }

        [Required]
        [Display(Name = "Can Instructor Do A Line By Line Review?")]
        public bool InstructorCanReview { get; set; }

        
        //NEED RUBRIC
    }
}