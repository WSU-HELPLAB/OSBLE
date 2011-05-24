using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace OSBLE.Models
{
    public class Course
    {
        [Required]
        [Key]
        public int ID { get; set; }

        // Basic Course Info

        [Required(AllowEmptyStrings=true)]
        [MaxLength(8)]
        [Display(Name = "Course Prefix")]
        public string Prefix { get; set; }

        [Required(AllowEmptyStrings = true)]
        [MaxLength(8)]
        [Display(Name = "Course Number")]
        public string Number { get; set; }

        [Required]
        [Display(Name = "Course Name")]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = true)]
        [MaxLength(8)]
        [Display(Name = "Semester")]
        public string Semester { get; set; }

        [Required(AllowEmptyStrings = true)]
        [MaxLength(4)]
        [Display(Name = "Year")]
        public string Year { get; set; }

        // Course Options

        [Display(Name = "This course is a community page. (No Assignments or Grades)")]
        public bool IsCommunity { get; set; }

        [Display(Name = "Allow students to post new threads in activity feed")]
        public bool AllowDashboardPosts { get; set; }

        [Display(Name = "Allow students to reply to threads posted in activity feed")]
        public bool AllowDashboardReplies { get; set; }

        [Display(Name = "Allow students to post events in course calendar")]
        public bool AllowEventPosting { get; set; }

        [Display(Name = "Require instructor to approve student calendar items to appear in the calendar")]
        public bool RequireInstructorApprovalForEventPosting { get; set; }

        [Display(Name = "Amount of weeks into the future to show events in course calendar")]
        public int CourseCalendarWindowOfTime { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Community Options

        [Display(Name = "Community Description")]
        [Required(AllowEmptyStrings=true)]
        public string CommunityDescription { get; set; }

        // References

        [Display(Name = "Course Weight")]
        public virtual ICollection<Weight> Weights { get; set; }

        [Display(Name = "Course Meeting Times")]
        public virtual ICollection<CourseMeeting> CourseMeetings { get; set; }

        [Display(Name = "Course Breaks")]
        public virtual ICollection<CourseBreak> CourseBreaks { get; set; }

        public Course() : base()
        {
            // Set default values for course settings.
            IsCommunity = false;
            AllowDashboardPosts = true;
            AllowDashboardReplies = true;
            AllowEventPosting = true;
            RequireInstructorApprovalForEventPosting = true;

            CourseCalendarWindowOfTime = 2;

            StartDate = DateTime.Now.Date;
            EndDate = DateTime.Now.Date.AddDays(112); // Add 16 weeks.
        }
    }
}