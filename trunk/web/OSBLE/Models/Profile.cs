using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;

namespace OSBLE.Models
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
    }
}