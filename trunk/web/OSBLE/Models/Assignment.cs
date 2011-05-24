using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Assignment : AbstractTask
    {

        [Required]
        [Display(Name = "AssignmentActivities")]
        public ICollection<AssignmentActivity> AssignmentActivities { get; set; }
    }
}