using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using OSBLE.Models.Users;

namespace OSBLE.Models.HomePage
{
    public abstract class AbstractDashboard
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        public int UserProfileID { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        [AllowHtml]
        [Required]
        [StringLength(4000)]
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
