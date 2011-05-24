using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public abstract class AssignmentActivity
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Weight")]
        public int Points { get; set; }
    }
}