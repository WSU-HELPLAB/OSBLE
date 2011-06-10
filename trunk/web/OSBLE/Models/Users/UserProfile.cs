using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities;
using System.Collections.Generic;
using OSBLE.Models.Assignments.Activities.Scores;

namespace OSBLE.Models.Users
{
    public class UserProfile
    {
        [Required]
        [Key]
        public int ID { get; set; }

        public string UserName { get; set; }

        public int SchoolID { get; set; }

        public virtual School School { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Identification { get; set; }

        public bool IsAdmin { get; set; }

        public bool CanCreateCourses { get; set; }

        public int DefaultCourse { get; set; }

        public ICollection<Score> Scores { get; set; }

        // User E-mail Notification Settings

        public bool EmailAllNotifications { get; set; }
     
        public UserProfile()
            : base()
        {
            IsAdmin = false;
            CanCreateCourses = false;
            DefaultCourse = 0;
        }
    }
}