using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;
using OSBLE.Models.HomePage;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.Assignments
{
    public class Assignment
    {
        public Assignment()
        {
            ReleaseDate = DateTime.UtcNow;
            DueDate = DateTime.UtcNow.AddDays(7.0);
            Deliverables = new List<Deliverable>();
            AssignmentTeams = new List<AssignmentTeam>();
            DiscussionTeams = new List<DiscussionTeam>();
            ReviewTeams = new List<ReviewTeam>();
            IsDraft = true;
            Type = AssignmentTypes.Basic;
            CriticalReviewPublishDate = null;
            ABETOutcomes = new List<AbetAssignmentOutcome>();
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

        [Required(ErrorMessage="Please indicate whether or not submission can be annotated.")]
        [Display(Name= "The instructor will annotate submissions (PDF only)")]
        public bool IsAnnotatable { get; set; }

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

        [Display(Name = "ABET Outcomes")]
        public virtual IList<AbetAssignmentOutcome> ABETOutcomes { get; set; }

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

        /// <summary>
        /// The department name for ABET assessment. If this value is null then it is implied 
        /// that ABET assessment is not being done for the course (at least not within OSBLE).
        /// </summary>
        public string ABETDepartment { get; set; }

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


        /// <summary>
        /// Returns how many students completed their team evaluations
        /// </summary>
        /// <returns></returns>
        public int GetCompletedTeamEvaluationsCount()
        {
            
            int returnVal = 0;
            using (ContextBase db = new SimpleContext())
            {
                //X teamevaluations are completed from a user Y, where user Y's team from the preceding assignment was of size X.
                //But, we want to consider one group of teamevaluations submitted by user Y as 1 team evaluation complete. 
                returnVal = (from te in db.TeamEvaluations
                                         where te.TeamEvaluationAssignmentID == this.ID
                                         select te.EvaluatorID).Distinct().Count();
            }
            return returnVal;
        }

        /// <summary>
        /// Returns how many total team evaluations there are.
        /// </summary>
        /// <returns></returns>
        public int GetTotalTeamEvaluationCount()
        {
            //get the number of all team members for this assignment. 
            int totalEvals = this.PreceedingAssignment.AssignmentTeams.Select(at => at.Team.TeamMembers.Count).Sum();
            return totalEvals;
        }


        /// <summary>
        /// Returns how many total initial posts have been made by students for this assignment.
        /// </summary>
        /// <returns></returns>
        public int GetInitialPostsCount()
        {
            int returnVal = 0;
            if (this.Type == AssignmentTypes.CriticalReviewDiscussion || this.Type == AssignmentTypes.DiscussionAssignment)
            {
                using (ContextBase db = new SimpleContext())
                {
                    //Initial posts are posts that have no ParentPost, 
                    //in addition, we only want to count each CourseUser once
                        //and only want to count students (as initial posts by non-students are not important)
                    returnVal = (from p in db.DiscussionPosts
                                 where p.ParentPostID == null &&
                                 p.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student &&
                                 p.AssignmentID == this.ID
                                 select p.CourseUserID).Distinct().Count();

                }
            }
            return returnVal;
        }

        /// <summary>
        /// Returns the number of new posts across all teams for the given user, for this assignment
        /// </summary>
        /// <returns></returns>
        public int GetNewPostsCount(int currentCourseUserId)
        {
            int returnVal = 0;

            if (this.Type == AssignmentTypes.CriticalReviewDiscussion || this.Type == AssignmentTypes.DiscussionAssignment)
            {
                using (ContextBase db = new SimpleContext())
                {
                    if (this.HasDiscussionTeams)
                    {
                        //Sum new posts across all teams
                        foreach (DiscussionTeam dt in this.DiscussionTeams)
                        {
                            returnVal += dt.GetNewPostsCount(currentCourseUserId);
                        }
                    }
                    else //Class wide discussion
                    {
                        //Get new posts for any discussion team, since the whole class is posting together.
                        returnVal = this.DiscussionTeams.FirstOrDefault().GetNewPostsCount(currentCourseUserId);
                    }
                }
            }
            return returnVal;
        }

        /// <summary>
        /// Returns the number of posts across all discussion teams for this assignment
        /// </summary>
        /// <returns></returns>
        public int GetPostsCount()
        {
            int returnVal = 0;
            if (this.Type == AssignmentTypes.CriticalReviewDiscussion || this.Type == AssignmentTypes.DiscussionAssignment)
            {
                using (ContextBase db = new SimpleContext())
                {
                    returnVal = (from dp in db.DiscussionPosts
                                  where dp.AssignmentID == this.ID
                                  select dp).Count();
                }
            }
            return returnVal;
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



        #endregion
    }
}
