﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Community : AbstractCourse
    {
        // Community Options

        [Display(Name = "Community Description")]
        [Required]
        public string Description { get; set; }

        [Display(Name = "Enter a short (3-4 character) nickname to display in the dashboard for this community")]
        [Required]
        public string Nickname { get; set; }

        [Display(Name = "Allow all community members to post events in calendar")]
        public bool AllowEventPosting { get; set; }


    }
}