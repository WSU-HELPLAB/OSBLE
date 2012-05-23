using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.HomePage;
using System.Runtime.Serialization;

namespace OSBLE.Models.Courses
{
    [KnownType(typeof(Community))]
    [KnownType(typeof(Course))]
    public abstract class AbstractCourse
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage="The course must have a name")]
        [Display(Name = "Name")]
        [StringLength(100)]
        public string Name { get; set; }

        public abstract bool AllowEventPosting { get; set; }

        [Display(Name = "Allow students to post new threads in activity feed")]
        public bool AllowDashboardPosts { get; set; }

        [Display(Name = "Amount of weeks into the future to show events in calendar")]
        public int CalendarWindowOfTime { get; set; }

        public AbstractCourse()
            : base()
        {
            CalendarWindowOfTime = 2;
        }
    }
}