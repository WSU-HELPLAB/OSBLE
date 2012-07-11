using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class TeamEvaluationSettings
    {
        [Key]
        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        /// <summary>
        /// The maximum allowed multiplier that can be used when team evaluations impact a student's
        /// grade.  Ex: after student evaluations, Stu has a grade of 150%.  If the maximum
        /// multiplier was 1.35, then Stu's grade would get chopped to 135%.
        /// </summary>
        [Display(Name="Maximum multiplier allowed per student")]
        [Required(AllowEmptyStrings=true, ErrorMessage="Please enter a multiplier.")]
        public double MaximumMultiplier { get; set; }

        /// <summary>
        /// If <see cref="DiscrepancyCheckSize"/> is set and a student's review of another student
        /// exceeds that amount, then require the evaluator to submit a comment of this lenght.
        /// Setting this to zero will ignore this setting.
        /// </summary>
        [Display(Name="Require an explanation of the specified length when a discrepancy occurs")]
        [Required(AllowEmptyStrings = true, ErrorMessage = "Please enter an explanation length (use 0 if not necessary).")]
        public int RequiredCommentLength { get; set; }

        /// <summary>
        /// If not zero, instructors will be notified if an evaluation exceeds the specified amount.
        /// Example: Stu giving Bob a 75% when this is set to 20 would set a flag.
        /// </summary>
        [Display(Name="Notify instructor if evaluation is more/less than the following amount")]
        [Required(AllowEmptyStrings = true, ErrorMessage = "Please enter a notification amount (use 0 if not necessary).")]
        public int DiscrepancyCheckSize { get; set; }

        public TeamEvaluationSettings()
        {
        }

        public TeamEvaluationSettings(TeamEvaluationSettings other)
            : this()
        {
            if (other == null)
            {
                return;
            }
            this.AssignmentID = other.AssignmentID;
            this.DiscrepancyCheckSize = other.DiscrepancyCheckSize;
            this.MaximumMultiplier = other.MaximumMultiplier;
            this.RequiredCommentLength = other.RequiredCommentLength;
        }
    }
}