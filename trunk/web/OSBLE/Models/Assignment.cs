using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Assignment
    {
        [Key]
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "AssignmentActivities")]
        public ICollection<AssignmentActivity> AssignmentActivities { get; set; }

        [Required]
        [Display(Name = "Weight")]
        public int Points { get; set; }
    }
}