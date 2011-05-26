using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
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
        [Display(Name = "PossiblePoints")]
        public int PossiblePoints { get; set; }
    }
}