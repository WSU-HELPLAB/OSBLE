using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;

namespace OSBLE.Models.HomePage
{
    
    public abstract class AbstractDashboard 
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        public int CourseUserID { get; set; }

        public virtual CourseUser CourseUser { get; set; }

        [Obsolete("For legacy compatibility.  You should be using the CourseUser property instead.")]
        public int UserProfileID
        {
            get
            {
                if (CourseUser != null)
                {
                    return CourseUser.UserProfileID;
                }
                return 0;
            }
        }

        [Obsolete("For legacy compatibility.  You should be using the CourseUser property instead.")]
        public UserProfile UserProfile
        {
            get
            {
                return CourseUser.UserProfile;
            }
        }

        [AllowHtml]
        [Required]
        public string Content { get; set; }

        // User's name to show (anonymized on the controller)
        [NotMapped]
        public string DisplayName { get; set; }

        [NotMapped]
        public string DisplayTitle { get; set; }

        [NotMapped]
        public bool ShowProfilePicture { get; set; }

        [NotMapped]
        public bool CanMail { get; set; }

        [NotMapped]
        public bool CanDelete { get; set; }

        public AbstractDashboard()
            : base()
        {
            Posted = DateTime.Now;

            CanMail = false;
            CanDelete = false;
            ShowProfilePicture = false;
            DisplayTitle = "";
            DisplayName = "";
        }
    }
}
