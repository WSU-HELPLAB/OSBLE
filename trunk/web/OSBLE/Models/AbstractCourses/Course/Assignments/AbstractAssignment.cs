using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.AbstractCourses;
using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;

namespace OSBLE.Models.Assignments
{
    public abstract class AbstractAssignment
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required(ErrorMessage="Please enter an assignment name.")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Description")]
        [StringLength(4000)]
        public string Description { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public virtual Category Category { get; set; }

        [Required]
        public virtual ICollection<AbstractAssignmentActivity> AssignmentActivities { get; set; }

        /// <summary>
        /// Grading points possible relative to sibling assignments in the parent category.
        /// </summary>
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

        public bool IsDraft { get; set; }

        public int? RubricID { get; set; }

        public virtual Rubric Rubric { get; set; }

        public int? CommentCategoryConfigurationID { get; set; }

        public virtual CommentCategoryConfiguration CommentCategoryConfiguration { get; set; }
    }
}