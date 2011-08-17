using System;
using System.ComponentModel.DataAnnotations;
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
        [Display(Name = "Starting date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [NotMapped]
        [Display(Name = "Starting time")]
        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [Display(Name="Ending date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [NotMapped]
        [Display(Name = "Ending time")]
        [DataType(DataType.Time)]
        public DateTime EndTime { get; set; }

        [Required]
        [Display(Name = "Event Title")]
        [StringLength(100)]
        public string Title { get; set; }

        [Display(Name = "Description (Optional)")]
        [StringLength(500)]
        public string Description { get; set; }

        public bool Approved { get; set; }

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
        }
    }
}