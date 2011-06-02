using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Gradables.StudioAssignment.Activities;

namespace OSBLE.Models.Gradables.StudioAssignment
{
    public class StudioAssignment
    {
        public StudioAssignment()
        {
            AssignmentActivities = new List<IAssignmentActivity>();
        }

        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        public virtual ICollection<IAssignmentActivity> AssignmentActivities { get; set; }
    }
}