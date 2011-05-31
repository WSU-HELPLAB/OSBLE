using System;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Models.HomePage
{
    public class Event
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int CourseID { get; set; }

        public virtual AbstractCourse Course { get; set; }

        // User who created the event. Optional.
        public int PosterID { get; set; }

        public virtual UserProfile Poster { get; set; }

        [Required]
        [Display(Name="Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [NotMapped]
        [Display(Name = "Time")]
        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [NotMapped]
        public DateTime EndDate { get; set; }

        [Required]
        [Display(Name = "Event Title")]
        public string Title { get; set; }

        [Display(Name = "Description (Optional)")]
        public string Description { get; set; }

        [Display(Name = "Link Title (Optional)")]
        public string LinkTitle { get; set; }

        [Display(Name = "Link (Optional)")]
        public string Link { get; set; }

        public bool Approved { get; set; }

        [NotMapped]
        public bool AllowLinking { get; set; }

        [NotMapped]
        public bool HideTime { get; set; }

        [NotMapped]
        public bool HideDelete { get; set; }

        [NotMapped]
        public bool NoDateTime { get; set; }

        public Event()
            : base()
        {
            StartDate = DateTime.Now.Date;

            NoDateTime = false;

            HideDelete = false;
            HideTime = false;
            AllowLinking = false;
        }
    }
}
