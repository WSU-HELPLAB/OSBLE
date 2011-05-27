using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Event
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int CourseID { get; set; }
        public virtual AbstractCourse Course { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public string Link { get; set; }

        public bool Approved { get; set; }

        public Event()
            : base()
        {
            Posted = DateTime.Now;
        }
    }
}