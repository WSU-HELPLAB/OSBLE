using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    /// <summary>
    /// This is the coursework weights
    /// </summary>
    public class Weight
    {
        [Key]
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Weight")]
        public int Points { get; set; }

        [Required]
        [Display(Name = "Assignments")]
        public ICollection<Assignment> Assignments { get; set; }
    }
}