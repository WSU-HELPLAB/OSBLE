using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OSBLE.Models
{
    public class CourseRole
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Display(Name="Can modify a course")]
        public bool CanModify;
        [Required]
        [Display(Name = "Can grade a course")]
        public bool CanGrade;
        [Required]
        [Display(Name = "Can moderate reviews in a course")]
        public bool CanModerate;
        [Required]
        [Display(Name = "Can submit assignments in a course")]
        public bool CanSubmit;
        [Required]
        [Display(Name = "All users in the course will appear anonymous to this user")]
        public bool Anonymized;
    }
}