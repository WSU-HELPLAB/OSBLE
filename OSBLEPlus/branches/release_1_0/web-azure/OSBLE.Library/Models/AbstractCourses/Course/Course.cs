using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;

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
        [Display(Name = "Term")]
        public string Semester { get; set; }

        [Required]
        [StringLength(4)]
        [Display(Name = "Year")]
        public string Year { get; set; }

        


        // Course Options

        [Display(Name = "Allow students to reply to threads posted in activity feed")]
        public bool AllowDashboardReplies { get; set; }

        [Display(Name = "Allow students to post events in course calendar")]
        public override bool AllowEventPosting { get; set; }

        [Display(Name = "Require an instructor to approve student(s) calendar events before they appear in the course calendar")]
        public bool RequireInstructorApprovalForEventPosting { get; set; }

        [Display(Name = "Course is inactive (only instructors/observers can log in)")]
        public bool Inactive { get; set; }

        [Display(Name = "Released:")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Due")]
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

        [Display(Name = "Course Meeting Times")]
        public virtual ICollection<CourseMeeting> CourseMeetings { get; set; }

        [Display(Name = "Course Time Zone offset")]
        public int TimeZoneOffset { get; set; }
        
        [Display(Name = "Course Breaks")]
        public virtual ICollection<CourseBreak> CourseBreaks { get; set; }

        [Display(Name = "Include course meetings and breaks in course calendar")]
        public bool ShowMeetings { get; set; }

        
        public virtual IList<Assignment> Assignments { get; set; }

        public Course()
            : base()
        {

            Assignments = new List<Assignment>();

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

            StartDate = DateTime.UtcNow.Date;
            EndDate = DateTime.UtcNow.Date.AddDays(112); // Add 16 weeks.
        }

        /// <summary>
        /// Copy constructor does not handle virtual members.
        /// </summary>
        /// <param name="copyCourse"></param>
        public Course(Course copyCourse)
            : this()
        {
            this.AllowDashboardPosts = copyCourse.AllowDashboardPosts;
            this.AllowDashboardReplies = copyCourse.AllowDashboardReplies;
            this.AllowEventPosting = copyCourse.AllowEventPosting;
            this.CalendarWindowOfTime = copyCourse.CalendarWindowOfTime;
            this.EndDate = copyCourse.EndDate;
            this.HoursLatePerPercentPenalty = copyCourse.HoursLatePerPercentPenalty;
            this.HoursLateUntilZero = copyCourse.HoursLateUntilZero;
            this.ID = copyCourse.ID;
            this.Inactive = copyCourse.Inactive;
            this.MinutesLateWithNoPenalty = copyCourse.MinutesLateWithNoPenalty;
            this.Name = copyCourse.Name;
            this.Number = copyCourse.Number;
            this.PercentPenalty = copyCourse.PercentPenalty;
            this.Prefix = copyCourse.Prefix;
            this.RequireInstructorApprovalForEventPosting = copyCourse.RequireInstructorApprovalForEventPosting;
            this.Semester = copyCourse.Semester;
            this.ShowMeetings = copyCourse.ShowMeetings;
            this.StartDate = copyCourse.StartDate;
            this.Year = copyCourse.Year;
            this.TimeZoneOffset = copyCourse.TimeZoneOffset;
        }

    }
}