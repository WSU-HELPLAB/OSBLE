using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.Courses
{
    /// <summary>
    /// This is the coursework weights
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Constructor method.  Will set the position to 0.
        /// </summary>
        public Category()
        {
            ColumnOrder = 0;
            Assignments = new List<AbstractAssignment>();
        }

        [Key]
        public int ID { get; set; }

        //really, the PK should be the course ID and the name of the weight, but it is my understanding
        //that this isn't possible in EF 4.1
        [Required]
        public int CourseID { get; set; }

        public virtual Course Course { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Weight")]
        public double Points { get; set; }

        /// <summary>
        /// Grading points possible relative to sibling categories in the parent course.
        /// </summary>
        public double PointsPossible { get; set; }

        /// <summary>
        /// Used for visual ordering of various categories (tabs in the gradebook).  Defaults to a value of 0.
        /// </summary>
        public int ColumnOrder { get; set; }

        /// <summary>
        /// Used for coloring the tab. Default to White.
        /// </summary>
        public string TabColor { get; set; }

        [Required]
        [Display(Name = "Gradables")]
        public ICollection<AbstractAssignment> Assignments { get; set; }

    }
}