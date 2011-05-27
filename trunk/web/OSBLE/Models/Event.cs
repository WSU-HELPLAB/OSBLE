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
        public DateTime StartDate { get; set; }

        [NotMapped]
        public DateTime EndDate { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public string Link { get; set; }

        public bool Approved { get; set; }

        [NotMapped]
        public bool AllowLinking { get; set; }

        [NotMapped]
        public bool HideTime { get; set; }

        [NotMapped]
        public bool HideDelete { get; set; }

        public Event()
            : base()
        {
            StartDate = DateTime.Now;

            HideDelete = false;
            HideTime = false;
            AllowLinking = false;
        }
    }
}