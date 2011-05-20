using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public abstract class AssignmentActivity
    {
        [Key]
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Weight")]
        public int Points { get; set; }

        public bool HasBeenGraded { get; set; }

        public int Grade { get; set; }
    }
}