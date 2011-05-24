using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Gradable : AbstractGradable
    {
        [Required]
        public ICollection<GradableScore> GradableScores { get; set; }
    }
}