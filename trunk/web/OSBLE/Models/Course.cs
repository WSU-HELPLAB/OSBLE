using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Course
    {
        [Required]
        [Key]
        public int ID { get; set; }

        // Basic Course Info

        [Required]
        [Display(Name = "Course Prefix")]
        public string Prefix { get; set; }

        [Required]
        [Display(Name = "Course Number")]
        public string Number { get; set; }

        [Required]
        [Display(Name = "Course Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Semester")]
        public string Semester { get; set; }

        [Required]
        [Display(Name = "Year")]
        public string Year { get; set; }

        // Course Options

        [Display(Name = "This course is a community page. (No Assignments or Grades)")]
        public bool IsCommunity { get; set; }

        [Display(Name = "Only Allow Instructors and TAs to make new posts to the activity feed")]
        public bool InstructorOnlyDashboardPost { get; set; }

        // References

        [Display(Name = "Course Weight")]
        public ICollection<Weight> Weights { get; set; }

        public Course() : base()
        {
            // Set default values for course settings.
            IsCommunity = false;
            InstructorOnlyDashboardPost = false;
        }
    }
}