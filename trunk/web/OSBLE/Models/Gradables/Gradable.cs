using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Gradables
{
    public class Gradable : AbstractGradable
    {
        [Required]
        public ICollection<GradableScore> GradableScores { get; set; }
    }
}