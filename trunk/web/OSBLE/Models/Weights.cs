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
        public int ID { get; set; }

        //really, the PK should be the course ID and the name of the weight, but it is my understanding
        //that this isn't possible in EF 4.1
        [Required]
        public int CourseID { get; set; }
        public virtual Course Course { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Weight")]
        public int Points { get; set; }

        [Required]
        [Display(Name = "Tasks")]
        public ICollection<AbstractTask> Tasks { get; set; }
    }
}