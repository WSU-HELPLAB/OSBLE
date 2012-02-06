using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;

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

        // User E-mail Notification Settings

        public bool EmailAllNotifications { get; set; }

        /// <summary>
        /// If set, will email all activity feed posts to the users
        /// </summary>
        public bool EmailAllActivityPosts { get; set; }

        public enum sortEmailBy
        {
            POSTED = 0,
            CONTEXT = 1,
            FROM = 2,
            SUBJECT = 3
        }

        public int SortBy { get; set; }

        public UserProfile()
            : base()
        {
            IsAdmin = false;
            CanCreateCourses = false;
            DefaultCourse = 0;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="up"></param>
        public UserProfile(UserProfile up)
            : base()
        {
            this.CanCreateCourses = up.CanCreateCourses;
            this.DefaultCourse = up.DefaultCourse;
            this.EmailAllNotifications = up.EmailAllNotifications;
            this.FirstName = up.FirstName;
            this.ID = up.ID;
            this.Identification = up.Identification;
            this.IsAdmin = up.IsAdmin;
            this.LastName = up.LastName;
            this.School = up.School;
            this.SchoolID = up.SchoolID;
            this.UserName = up.UserName;
            this.SortBy = up.SortBy;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", FirstName, LastName);
        }

        public string LastAndFirst()
        {
            return string.Format("{0}, {1}", LastName, FirstName);
        }
    }
}