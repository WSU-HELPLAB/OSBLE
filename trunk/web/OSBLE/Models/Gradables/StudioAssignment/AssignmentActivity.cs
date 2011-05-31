using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Gradables.StudioAssignment
{
    public abstract class AssignmentActivity
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public int AssignmentID { get; set; }

        public virtual SubmissionActivitySettings Assignment { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Weight")]
        public int Points { get; set; }
    }
}