using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Submission
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public ICollection<string> SubmittedFiles { get; set; }

        [Required]
        public bool isGraded { get; set; }

        public int score { get; set; }
    }
}