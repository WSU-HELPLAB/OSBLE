using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.AbstractCourses.Course;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class Assignment
    {
        public Assignment()
        {
            ReleaseDate = DateTime.Now;
            DueDate = DateTime.Now.AddDays(7.0);
            ColumnOrder = 0;
        }

        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Please specify this assignment's type")]
        [Display(Name = "Assignment Type")]
        public AssignmentType AssignmentType { get; set; }

        [Required(ErrorMessage = "Please specify an assignment name")]
        [Display(Name = "Assignment Name")]
        public string AssignmentName { get; set; }

        [Required(ErrorMessage = "Please provide an assignment description")]
        [Display(Name = "Assignment Description")]
        public string AssignmentDescription { get; set; }

        [Required(ErrorMessage = "Please select the category to which this assignment will belong")]
        [Display(Name = "Assignment Category")]
        public int CategoryID { get; set; }
        public virtual Category Category { get; set; }

        [Required(ErrorMessage = "Please specify the total number of points that this assignment will be worth")]
        [Display(Name = "Total Points Possible")]
        public int PointsPossible { get; set; }

        [Required(ErrorMessage = "Please specify when this assignment should be released")]
        [Display(Name = "Release Date")]
        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }

        [NotMapped]
        [DataType(DataType.Time)]
        public DateTime ReleaseTime 
        {
            get
            {
                return ReleaseDate;
            }
            set
            {
                //first, zero out the release date's time component
                ReleaseDate = DateTime.Parse(ReleaseDate.ToShortDateString());
                ReleaseDate.AddHours(value.Hour);
                ReleaseDate.AddMinutes(value.Minute);
            }
        }

        [Required(ErrorMessage = "Please specify when this assignment should be due")]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [NotMapped]
        [DataType(DataType.Time)]
        public DateTime DueTime 
        {
            get
            {
                return DueDate;
            }
            set
            {
                //first, zero out the date's time component
                DueDate = DateTime.Parse(DueDate.ToShortDateString());
                DueDate.AddHours(value.Hour);
                DueDate.AddMinutes(value.Minute);
            }
        }

        [Required(ErrorMessage = "Please specify for how long OSBLE should accept late submissions")]
        [Display(Name = "Late Assignment Window (Hours)")]
        public int HoursLateWindow { get; set; }

        [Required(ErrorMessage = "Please specify the percent that should be deducted per hour late")]
        [Display(Name = "Percent Deduction Per Late Hour")]
        public double DeductionPerHourLate { get; set; }

        [Required]
        public int ColumnOrder { get; set; }

        [Required(ErrorMessage = "Please specify whether or not this assignment is a draft")]
        [Display(Name = "Safe As Draft")]
        public bool IsDraft { get; set; }

        public int? RubricID { get; set; }
        public virtual Rubric Rubric { get; set; }

        public int? CommentCategoryID { get; set; }
        public virtual CommentCategoryConfiguration CommentCategory { get; set; }

        public int? PrecededBy  { get; set; }

        [Association("PrecedingAssignment", "PrecededBy", "ID")]
        public virtual Assignment PrecedingAssignment { get; set; }

        [Association("Deliverables", "ID", "AssignmentID")]
        public virtual IList<Deliverable> Deliverables { get; set; }
    }
}