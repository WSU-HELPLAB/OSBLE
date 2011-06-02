using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;

namespace OSBLE.Models.Gradables
{
    public abstract class AbstractGradable
    {
        /// <summary>
        /// Constructor method.  Will set the position to 0.
        /// </summary>
        public AbstractGradable()
        {
            Position = 0;
        }

        [Key]
        [Required]
        [Display(Name = "ID")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        public int WeightID { get; set; }

        public virtual Weight Weight { get; set; }

        /// <summary>
        /// Used for visual ordering of various weights (columns in the gradebook).  Defaults to a value of 0.
        /// </summary>
        [Required]
        public int Position { get; set; }

        [Required]
        [Display(Name = "Possible Points")]
        public int PossiblePoints { get; set; }

        // Late Policy
        [Required]
        [Display(Name = "Minutes Late With No Penalty")]
        public int MinutesLateWithNoPenalty { get; set; }

        [Required]
        [Range(0, 100)]
        [Display(Name = "Percent Penalty")]
        public int PercentPenalty { get; set; }

        [Required]
        [Display(Name = "Hours Late Per Percent Penalty")]
        public int HoursLatePerPercentPenalty { get; set; }

        [Required]
        [Display(Name = "Hours Late Until Zero")]
        public int HoursLateUntilZero { get; set; }

    }
}