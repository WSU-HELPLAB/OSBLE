using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class TaskScore
    {
        [Key]
        [Required]
        [Column(Order = 0)]
        public int UserProfileID { get; set; }
        public virtual UserProfile UserProfile { get; set; }

        [Key]
        [Required]
        [Column(Order = 1)]
        public int TaskID { get; set; }
        public virtual Task Task { get; set; }

        [Required]
        public int Score { get; set; }
    }
}