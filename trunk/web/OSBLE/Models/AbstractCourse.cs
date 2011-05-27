using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public abstract class AbstractCourse
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        public virtual bool AllowEventPosting { get; set; }

        public virtual ICollection<Event> Events { get; set; }

        [Display(Name = "Amount of weeks into the future to show events in calendar")]
        public int CalendarWindowOfTime { get; set; }

        public AbstractCourse() : base()
        {
            CalendarWindowOfTime = 2;
        }
    }
}