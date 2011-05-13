using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public enum OSBLERoles : int
    {
        Instructor = 1,
        TA,
        Student,
        Moderator,
        Observer
    }

    public class CourseRole
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
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

        public CourseRole()
            : base()
        {
        }

        public CourseRole(string Name, bool CanModify, bool CanSeeAll, bool CanGrade, bool CanSubmit, bool Anonymized)
            : base()
        {
            this.Name = Name;
            this.CanModify = CanModify;
            this.CanSeeAll = CanSeeAll;
            this.CanGrade = CanGrade;
            this.CanSubmit = CanSubmit;
            this.Anonymized = Anonymized;
        }
    }
}