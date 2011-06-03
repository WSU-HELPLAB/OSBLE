using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class AbstractAssignment
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Assignment Name")]
        public string Name { get; set; }
    }
}