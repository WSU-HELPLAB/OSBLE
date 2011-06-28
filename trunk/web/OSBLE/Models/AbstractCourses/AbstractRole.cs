using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses
{
    public abstract class AbstractRole
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Can modify a course")]
        public bool CanModify { get; set; }

        [Required]
        [Display(Name = "Can see all course content")]
        public bool CanSeeAll { get; set; }

        [Required]
        [Display(Name = "Can grade a course")]
        public bool CanGrade { get; set; }

        [Required]
        [Display(Name = "Can submit assignments in a course")]
        public bool CanSubmit { get; set; }

        [Required]
        [Display(Name = "All users in the course will appear anonymous to this user")]
        public bool Anonymized { get; set; }

        [Required]
        [Display(Name = "Allow the user to upload files")]
        public bool CanUploadFiles { get; set; }
    }
}