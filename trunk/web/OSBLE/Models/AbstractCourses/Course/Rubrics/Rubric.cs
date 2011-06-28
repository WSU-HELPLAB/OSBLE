using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Rubrics
{
    public class Rubric
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public string Description { get; set; }

        public virtual ICollection<Level> Levels { get; set; }

        public virtual ICollection<Criterion> Criteria { get; set; }
    }
}