using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses
{
    public class LetterGrade
    {
        [Required]
        [Key]
        public int ID { get; set; }

        //should be 2 character array
        [Required]
        [Display(Name = "Letter Grade")]
        [StringLength(3)]
        public string Grade { get; set; }

        [Required]
        [Display(Name = "Minimum % Required")]
        public int MinimumRequired { get; set; }

        public LetterGrade()
        {
        }
    }
}