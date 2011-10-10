using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.AbstractCourses;
using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Users;
using System.Linq;

namespace OSBLE.Models.Assignments
{
    public abstract class AbstractAssignment
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required(ErrorMessage="Please enter an assignment name.")]
        [StringLength(100, ErrorMessage="The assignment's name must be under 100 characters.")]
        [Display(Name = "Assignment Name")]
        public string Name { get; set; }

        [Required(ErrorMessage="Please provide a description for this assignment.")]
        [Display(Name = "Description")]
        [StringLength(4000)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Grading Category")]
        public int CategoryID { get; set; }

        public virtual Category Category { get; set; }

        [Required]
        public virtual ICollection<AbstractAssignmentActivity> AssignmentActivities { get; set; }

        /// <summary>
        /// Grading points possible relative to sibling assignments in the parent category.
        /// </summary>
        [Display(Name = "Points Possible")]
        public int PointsPossible { get; set; }

        /// <summary>
        /// Used for visual ordering of assignments in the gradebook.  Defaults to a value of 0.
        /// </summary>
        public int ColumnOrder { get; set; }

        public AbstractAssignment()
        {
            ColumnOrder = 0;

            // Create Activities Collection if none exists
            if (AssignmentActivities == null)
            {
                AssignmentActivities = new List<AbstractAssignmentActivity>();
            }
        }

        [NotMapped]
        public bool IsTeamAssignment
        {
            get
            {
                foreach (AbstractAssignmentActivity activity in this.AssignmentActivities)
                {
                    if (activity.isTeam)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsDraft { get; set; }

        public int? RubricID { get; set; }

        public virtual Rubric Rubric { get; set; }

        public int? CommentCategoryConfigurationID { get; set; }

        public virtual CommentCategoryConfiguration CommentCategoryConfiguration { get; set; }

        public OldTeamMember GetUsersTeam(UserProfile user)
        {
            if (!IsTeamAssignment)
            {
                return new OldTeamMember();
            }

            //AC: Kind of dangerous, but for now, we just assume that the first team activity
            //is the one we want.  This might have to get reworked after we introduce review
            //activities
            AbstractAssignmentActivity activity = (from a in AssignmentActivities
                                                   where a.isTeam == true
                                                   select a).FirstOrDefault();
            OldTeamMember team = (from t in activity.TeamUsers
                               where t.Contains(user) == true
                               select t).FirstOrDefault() as OldTeamMember;
            if (team == null)
            {
                team = new OldTeamMember() { Team = new OldTeam()};
                team.Team.Name = "Empty Team";
            }
            return team;
        }
    }
}