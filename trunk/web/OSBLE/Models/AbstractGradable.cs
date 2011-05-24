using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace OSBLE.Models
{
    public abstract class AbstractGradable
    {
        [Key]
        [Required]
        [Display(Name = "ID")]
        public int ID { get; set; }

        [Required]
        public int WeightID { get; set; }
        public virtual Weight Weight { get; set; }

        [Required]
        [Display(Name = "Weight")]
        public int Points { get; set; }

    }
}