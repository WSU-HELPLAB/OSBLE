using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses
{
    public class AssessmentCommittee : AbstractCourse
    {
        // Committee Options

        [Display(Name = "Committee Description")]
        [StringLength(100)]
        [Required]
        public string Description { get; set; }

        [Display(Name = "Allow all assessment committee members to post events in calendar")]
        public override bool AllowEventPosting { get; set; }

        public AssessmentCommittee()
            : base()
        {
            AllowEventPosting = true;
            AllowDashboardPosts = true;
        }
    }
}