using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public abstract class AbstractGradable
    {
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

        [Required]
        [Display(Name = "PossiblePoints")]
        public int PossiblePoints { get; set; }
    }
}