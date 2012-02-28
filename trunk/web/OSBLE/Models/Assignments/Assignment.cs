using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;
using OSBLE.Models.HomePage;

namespace OSBLE.Models.Assignments
{
    public class Assignment
    {
        public Assignment()
        {
            ReleaseDate = DateTime.Now;
            DueDate = DateTime.Now.AddDays(7.0);
            ColumnOrder = 0;
            Deliverables = new List<Deliverable>();
            AssignmentTeams = new List<AssignmentTeam>();
            IsDraft = true;
            addedPoints = 0;
            IsWizardAssignment = true;
            Scores = new List<Score>();
        }

        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Please specify this assignment's type")]
        [Display(Name = "Assignment Type")]
        public string AssignmentType { get; set; }

        [ForeignKey("AssignmentType")]
        public AssignmentType Type { get; set; }

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
                ReleaseDate = ReleaseDate.AddHours(value.Hour);
                ReleaseDate = ReleaseDate.AddMinutes(value.Minute);
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
                
                DueDate = DueDate.AddHours(value.Hour);
                DueDate = DueDate.AddMinutes(value.Minute);
            }
        }

        /// <summary>
        /// Returns true if the Assignment has an associated Rubric
        /// </summary>
        [NotMapped]
        public bool HasRubric
        {
            get
            {
                return RubricID != null;
            }
        }

        /// <summary>
        /// Returns true if the assignment is team-based
        /// </summary>
        [NotMapped]
        public bool HasTeams
        {
            get
            {
                foreach (AssignmentTeam at in AssignmentTeams)
                {
                    if (at.Team.TeamMembers.Count() > 1)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true if the assignment has comment categories
        /// </summary>
        [NotMapped]
        public bool HasCommentCategories
        {
            get
            {
                return CommentCategory != null;
            }
        }

        /// <summary>
        /// Returns true if the assignment has one or more deliverables
        /// </summary>
        [NotMapped]
        public bool HasDeliverables
        {
            get
            {
                return Deliverables.Count > 0;
            }
        }

        /// <summary>
        /// Returns an int with the amount of submissions received for the assignment
        /// </summary>
        public int GetSubmissionCount()
        {
            return FileSystem.GetFolderDocumentCount(this.Category.Course, this.ID);
        }

        /// <summary>
        /// Returns an int with the amount of scores that are currently saved as draft
        /// </summary>
        public int GetSavedAsDraftCount()
        {
            OSBLEContext db = new OSBLEContext();
            int draftRubricEvals = (from a in db.RubricEvaluations
                                    where a.AssignmentID == this.ID &&
                                    !a.IsPublished
                                    select a).Count();
            return draftRubricEvals;
        }

        /// <summary>
        /// Returns an int with the amount of scores that are currently Published
        /// </summary>
        /// <returns></returns>
        public int GetPublishedCount()
        {
            OSBLEContext db = new OSBLEContext();
            int draftRubricEvals = (from a in db.RubricEvaluations
                                    where a.AssignmentID == this.ID &&
                                    a.IsPublished
                                    select a).Count();
            return draftRubricEvals;
        }

        [Required(ErrorMessage = "Please specify for how long OSBLE should accept late submissions")]
        [Display(Name = "Late Submission Window")]
        public int HoursLateWindow { get; set; }

        [Required(ErrorMessage = "Please specify the percent that should be deducted per hour late")]
        [Display(Name = "Penalty of")]
        public double DeductionPerUnit { get; set; }

        [Required(ErrorMessage = "Please specify the hours per percent deduction.")]
        [Display(Name = "hour(s) late")]
        public double HoursPerDeduction { get; set; }

        [Required]
        public int ColumnOrder { get; set; }

        [Required(ErrorMessage = "Please specify whether or not this assignment is a draft")]
        [Display(Name = "Safe As Draft")]
        public bool IsDraft { get; set; }

        public int? RubricID { get; set; }
        public virtual Rubric Rubric { get; set; }
        
        public int? CommentCategoryID { get; set; }
        public virtual CommentCategoryConfiguration CommentCategory { get; set; }

        public int? PrecededingAssignmentID  { get; set; }

        [ForeignKey("PrecededingAssignmentID")]
        public virtual Assignment PreceedingAssignment { get; set; }

        [Association("Assignment_Deliverables", "ID", "AssignmentID")]
        public virtual IList<Deliverable> Deliverables { get; set; }

        [Association("AssignmentTeams_Assignments", "ID", "AssignmentID")]
        public virtual IList<AssignmentTeam> AssignmentTeams { get; set; }

        [Association("Score_Assignment", "ID", "AssignmentID")]
        public virtual IList<Score> Scores { get; set; }

        public double addedPoints { get; set; }

        public int? AssociatedEventID { get; set; }

        [ForeignKey("AssociatedEventID")]
        public virtual Event AssociatedEvent { get; set; }
        
        /// <summary>
        /// used to distinguish wizard created assignments versus gradebook created assignments
        /// </summary>
        public bool IsWizardAssignment { get; set; }
    }
}