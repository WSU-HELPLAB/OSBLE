﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
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