using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Assignment : AbstractGradable
    {

        [Required]
        [Display(Name = "AssignmentActivities")]
        public ICollection<AssignmentActivity> AssignmentActivities { get; set; }
    }
}