﻿using System;
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
            Deliverables = new List<Deliverable>();
            AssignmentTeams = new List<AssignmentTeam>();
            DiscussionTeams = new List<DiscussionTeam>();
            ReviewTeams = new List<ReviewTeam>();
            IsDraft = true;
            Type = AssignmentTypes.Basic;
            CriticalReviewPublishDate = null;
        }

        public Assignment(Assignment other)
            : this()
        {
            this.AssignmentDescription = other.AssignmentDescription;
            this.AssignmentName = other.AssignmentName;
            this.AssignmentTypeID = other.AssignmentTypeID;
            this.AssociatedEventID = other.AssociatedEventID;
            this.CommentCategoryID = other.CommentCategoryID;
            this.CourseID = other.CourseID;
            this.CriticalReviewPublishDate = other.CriticalReviewPublishDate;
            this.DeductionPerUnit = other.DeductionPerUnit;
            foreach (Deliverable d in other.Deliverables)
            {
                Deliverables.Add(new Deliverable(d));
            }
            this.DiscussionSettings = new DiscussionSetting(other.DiscussionSettings);
            this.CriticalReviewSettings = new CriticalReviewSettings(other.CriticalReviewSettings);
            this.DueDate = other.DueDate;
            this.HoursLateWindow = other.HoursLateWindow;
            this.HoursPerDeduction = other.HoursPerDeduction;
            this.ID = other.ID;
            this.IsDraft = other.IsDraft;
            this.PrecededingAssignmentID = other.PrecededingAssignmentID;
            this.ReleaseDate = other.ReleaseDate;
            this.RubricID = other.RubricID;
            this.StudentRubricID = other.StudentRubricID;
            this.TeamEvaluationSettings = new TeamEvaluationSettings(other.TeamEvaluationSettings);
            this.Type = other.Type;
        }

        #region public properties

        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "Please specify this assignment's type")]
        public int AssignmentTypeID { get; set; }


        [Display(Name = "Assignment Type")]
        [NotMapped]
        public AssignmentTypes Type
        {
            get
            {
                return (AssignmentTypes)AssignmentTypeID;
            }
            set
            {
                AssignmentTypeID = (int)value;
            }
        }

        [Required(ErrorMessage = "Please specify an assignment name")]
        [Display(Name = "Assignment Name")]
        public string AssignmentName { get; set; }

        [Required(ErrorMessage = "Please provide an assignment description")]
        [Display(Name = "Assignment Description")]
        public string AssignmentDescription { get; set; }

        public int? CourseID { get; set; }
        public virtual Course Course { get; set; }

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
        /// Returns true if the Assignment has an associated Student Rubric
        /// </summary>
        [NotMapped]
        public bool HasStudentRubric
        {
            get
            {
                return StudentRubricID != null;
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
        /// Returns true if the assignment has discussion teams
        /// </summary>
        [NotMapped]
        public bool HasDiscussionTeams
        {
            get
            {
                if (Type != AssignmentTypes.DiscussionAssignment && Type != AssignmentTypes.CriticalReviewDiscussion)
                {
                    return false;
                }
                int count = (from team in DiscussionTeams
                             where team.GetAllTeamMembers().Count > 1
                             select team).Count();
                if (count > 0)
                {
                    return true;
                }
                return false;
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


        [Required(ErrorMessage = "Please specify for how long OSBLE should accept late submissions")]
        [Display(Name = "Late Submission Window")]
        public int HoursLateWindow { get; set; }

        [Required(ErrorMessage = "Please specify the percent that should be deducted per hour late")]
        [Display(Name = "Penalty of")]
        public double DeductionPerUnit { get; set; }

        [Required(ErrorMessage = "Please specify the hours per percent deduction.")]
        [Display(Name = "hour(s) late")]
        public double HoursPerDeduction { get; set; }

        [Required(ErrorMessage = "Please specify whether or not this assignment is a draft")]
        [Display(Name = "Safe As Draft")]
        public bool IsDraft { get; set; }

        public int? RubricID { get; set; }
        public virtual Rubric Rubric { get; set; }

        public int? StudentRubricID { get; set; }
        public virtual Rubric StudentRubric { get; set; }

        public int? CommentCategoryID { get; set; }
        public virtual CommentCategoryConfiguration CommentCategory { get; set; }

        public int? PrecededingAssignmentID { get; set; }

        [ForeignKey("PrecededingAssignmentID")]
        public virtual Assignment PreceedingAssignment { get; set; }

        [Association("Deliverable_Assignment", "ID", "AssignmentID")]
        public virtual IList<Deliverable> Deliverables { get; set; }

        [Association("AssignmentTeam_Assignments", "ID", "AssignmentID")]
        public virtual IList<AssignmentTeam> AssignmentTeams { get; set; }

        [Association("DiscussionTeam_Assignments", "ID", "AssignmentID")]
        public virtual IList<DiscussionTeam> DiscussionTeams { get; set; }

        [Association("AssignmentReviewTeam_Assignment", "ID", "AssignmentID")]
        public virtual IList<ReviewTeam> ReviewTeams { get; set; }

        [Association("DiscussionSetting_Assignment", "ID", "AssignmentID")]
        public virtual DiscussionSetting DiscussionSettings { get; set; }

        [Association("CriticalReviewSettings_Assignment", "ID", "AssignmentID")]
        public virtual CriticalReviewSettings CriticalReviewSettings { get; set; }

        [Association("TeamEvaluationSettings_Assignment", "ID", "AssignmentID")]
        public virtual TeamEvaluationSettings TeamEvaluationSettings { get; set; }

        public int? AssociatedEventID { get; set; }

        [ForeignKey("AssociatedEventID")]
        public virtual Event AssociatedEvent { get; set; }

        /// <summary>
        /// Returns true if an assignment has had its reviewed documents published back to their original author(s)
        /// </summary>
        [NotMapped]
        public bool IsCriticalReviewPublished
        {
            get
            {
                return (CriticalReviewPublishDate != null);
            }
        }

        /// <summary>
        /// Returns the date of when a critical review assignment had its reviewed documents published back to their original author(s)
        /// </summary>
        public DateTime? CriticalReviewPublishDate { get; set; }

        #endregion

        #region public methods

        /// <summary>
        /// Returns an int with the amount of submissions received for the assignment
        /// </summary>
        public int GetSubmissionCount()
        {
            int returnVal = 0;
            foreach (AssignmentTeam team in this.AssignmentTeams)
            {
                if (team.GetSubmissionTime() != null)
                {
                    returnVal++;
                }
            }
            return returnVal;
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
        /// Returns an int with the amount of instructor evaluated rubrics that are currently saved as draft
        /// </summary>
        public int GetSavedAsDraftCount()
        {
            int draftRubricEvals = 0;
            using (OSBLEContext db = new OSBLEContext())
            {
                draftRubricEvals = (from a in db.RubricEvaluations
                                    where a.AssignmentID == this.ID &&
                                    !a.IsPublished &&
                                    a.Evaluator.AbstractRole.CanGrade
                                    select a).Count();
            }
            return draftRubricEvals;
        }

        /// <summary>
        /// Returns an int with the amount of instructor evaluated rubrics that are currently Published
        /// </summary>
        /// <returns></returns>
        public int GetPublishedCount()
        {
            //get number of items graded

            int draftRubricEvals = 0;
            using (ContextBase db = new SimpleContext())
            {
                draftRubricEvals = (from a in db.RubricEvaluations
                                    where a.AssignmentID == this.ID &&
                                    a.IsPublished &&
                                    a.Evaluator.AbstractRole.CanGrade
                                    select a).Count();
            }
            return draftRubricEvals;
        }

        #endregion

        #region static methods

        public static IList<AssignmentTypes> AllAssignmentTypes
        {
            get
            {
                return Enum.GetValues(typeof(AssignmentTypes)).Cast<AssignmentTypes>().ToList();
            }
        }

        public static void ToggleDraft(int assignmentId, int posterId)
        {
            using (ContextBase db = new SimpleContext())
            {
                //MG: Pulling the assignment from the DB, toggling its IsDraft parameter. and saving it back to the DB.
                Assignment assignment = db.Assignments.Find(assignmentId);
                assignment.IsDraft = !assignment.IsDraft;
                db.SaveChanges();

                if (assignment.IsDraft)
                {
                    if (assignment.AssociatedEvent != null)
                    {
                        Event e = db.Events.Find(assignment.AssociatedEventID);
                        db.Events.Remove(e);
                        db.SaveChanges();
                    }
                }
                else
                {
                    Event e = new Event()
                    {
                        Description = assignment.AssignmentDescription,
                        EndDate = assignment.DueDate,
                        EndTime = assignment.DueTime,
                        Approved = true,
                        PosterID = posterId,
                        StartDate = assignment.ReleaseDate,
                        StartTime = assignment.ReleaseTime,
                        Title = assignment.AssignmentName
                    };
                    db.Events.Add(e);
                    db.SaveChanges();
                    assignment.AssociatedEventID = e.ID;
                    db.SaveChanges();
                }
            }
        }

        #endregion
    }
}