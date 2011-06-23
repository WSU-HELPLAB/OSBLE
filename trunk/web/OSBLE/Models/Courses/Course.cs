using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses
{
    public class Course : AbstractCourse
    {
        // Basic Course Info

        [Required]
        [StringLength(8)]
        [Display(Name = "Prefix")]
        public string Prefix { get; set; }

        [Required]
        [StringLength(8)]
        [Display(Name = "Number")]
        public string Number { get; set; }

        [Required]
        [StringLength(8)]
        [Display(Name = "Semester")]
        public string Semester { get; set; }

        [Required]
        [StringLength(4)]
        [Display(Name = "Year")]
        public string Year { get; set; }

        // Course Options

        [Display(Name = "Allow students to post new threads in activity feed")]
        public bool AllowDashboardPosts { get; set; }

        [Display(Name = "Allow students to reply to threads posted in activity feed")]
        public bool AllowDashboardReplies { get; set; }

        [Display(Name = "Allow students to post events in course calendar")]
        public override bool AllowEventPosting { get; set; }

        [Display(Name = "Require instructor to approve student calendar items to appear in the calendar")]
        public bool RequireInstructorApprovalForEventPosting { get; set; }

        [Display(Name = "Course is inactive (only instructors/observers can log in)")]
        public bool Inactive { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        // Default Late Policy

        [Required]
        [Display(Name = "Minutes Late With No Penalty")]
        public int MinutesLateWithNoPenalty { get; set; }

        [Required]
        [Range(0, 100)]
        [Display(Name = "Percent Penalty")]
        public int PercentPenalty { get; set; }

        [Required]
        [Display(Name = "Hours Late Per Percent Penalty")]
        public int HoursLatePerPercentPenalty { get; set; }

        [Required]
        [Display(Name = "Hours Late Until Zero")]
        public int HoursLateUntilZero { get; set; }

        // References

        [Display(Name = "Course Categories")]
        public virtual ICollection<Category> Categories { get; set; }

        [Display(Name = "Course Meeting Times")]
        public virtual ICollection<CourseMeeting> CourseMeetings { get; set; }

        [Display(Name = "Course Breaks")]
        public virtual ICollection<CourseBreak> CourseBreaks { get; set; }

        [Display(Name = "Course Letter Grades")]
        public virtual ICollection<LetterGrade> LetterGrades { get; set; }

        [Display(Name = "Include course meetings and breaks in course calendar")]
        public bool ShowMeetings { get; set; }

        public Course()
            : base()
        {
            // Set default values for course settings.
            AllowDashboardPosts = true;
            AllowDashboardReplies = true;
            AllowEventPosting = true;
            RequireInstructorApprovalForEventPosting = false;
            Inactive = false;
            ShowMeetings = true;

            MinutesLateWithNoPenalty = 5;
            PercentPenalty = 10;
            HoursLatePerPercentPenalty = 24;
            HoursLateUntilZero = 48;

            StartDate = DateTime.Now.Date;
            EndDate = DateTime.Now.Date.AddDays(112); // Add 16 weeks.
        }
    }
}