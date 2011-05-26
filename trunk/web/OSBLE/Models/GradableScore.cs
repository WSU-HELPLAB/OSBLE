using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class GradableScore
    {
        [Key]
        [Required]
        [Column(Order = 0)]
        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }

        [Key]
        [Required]
        [Column(Order = 1)]
        public int GradableID { get; set; }
        public virtual Gradable Gradable { get; set; }

        [Required]
        public int Score { get; set; }
    }
}