using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;

namespace OSBLE.Models
{
    public class School
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [StringLength(128)]
        public string Name { get; set; }

        public ICollection<UserProfile> UserProfiles;
    }
}