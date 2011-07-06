using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Users;

namespace OSBLE.Models.AbstractCourses.Course
{
    public class Submission
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public int TeamUserID { get; set; }

        public virtual TeamUserMember TeamUser { get; set; }

        [Required]
        public int AbstractAssignmentActivityID { get; set; }

        public virtual AbstractAssignmentActivity Assignemnt { get; set; }
    }
}