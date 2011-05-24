using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Task : AbstractTask
    {
        [Required]
        [Display(Name = "TaskScores")]
        public ICollection<TaskScore> TaskScores { get; set; }
    }
}