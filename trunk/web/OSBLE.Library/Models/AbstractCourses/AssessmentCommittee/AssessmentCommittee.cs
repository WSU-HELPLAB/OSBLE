using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses
{
    public class AssessmentCommittee : AbstractCourse
    {
        // Committee Options

        [Display(Name = "Committee Description")]
        [StringLength(100)]
        [Required(ErrorMessage = "The committee must have a name")]
        public string Description { get; set; }

        [Display(Name = "Allow all assessment committee members to post events in calendar")]
        public override bool AllowEventPosting { get; set; }

        [Required]
        [Display(Name = "University")]
        public School University;

        [Required]
        [Display(Name = "Degree Programs Assessed")]
        public string DegreeProgramsAssessed;

        [Required]
        [Display(Name = "ABET Outcomes Assessed")]
        public string ABETOutcomesAssessed;

        [Required]
        [Display(Name = "Assessment Year Start")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        public AssessmentCommittee()
            : base()
        {
            AllowEventPosting = true;
            AllowDashboardPosts = true;
            StartDate = DateTime.UtcNow;
        }
    }
}