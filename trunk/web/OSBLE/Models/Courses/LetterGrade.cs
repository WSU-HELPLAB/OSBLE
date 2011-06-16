using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
        [Display(Name="Letter Grade")]
        public string Grade { get; set; }

        [Required]
        [Display(Name="Minimum % Required")]
        public int MinimumRequired { get; set; }

        public LetterGrade()
        {
        }
    }
}