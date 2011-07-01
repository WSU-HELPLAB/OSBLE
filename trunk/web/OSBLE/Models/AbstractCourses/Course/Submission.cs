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

        /// <summary>
        /// This says whether this was submitted for a team or user
        /// if team of users then User and UserID must not be null
        /// if team of teams then Team and TeamID must not be null
        /// </summary>
        ///
        [Required]
        public TeamOrUser TeamUser { get; set; }

        public int? UserProfileID { get; set; }

        public virtual UserProfile User
        {
            get;
            set;
        }

        public int? TeamID { get; set; }

        public virtual Team Team { get; set; }

        [Required]
        public int AbstractAssignmentActivityID { get; set; }

        public virtual AbstractAssignmentActivity Assignemnt { get; set; }
    }
}