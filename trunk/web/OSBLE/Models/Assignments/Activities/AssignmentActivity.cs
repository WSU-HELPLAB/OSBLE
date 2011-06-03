using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using OSBLE.Models.Assignments.Activities.Scores;

namespace OSBLE.Models.Assignments.Activities
{
    public abstract class AssignmentActivity
    {
        [Required]
        [Key]
        public int ID { get; set; }

        public string Name { get; set; }

        public AssignmentActivity()
        {
            DateTime dateTimeNow = DateTime.Now;

            ColumnOrder = 0;

            //The default is tomorrow at midnight
            ReleaseDate = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 0, 0, 0);
            ReleaseDate = ReleaseDate.AddDays(1);
        }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Release Date")]
        public DateTime ReleaseDate
        {
            get;
            set;
        }

        [Required]
        public int AbstractAssignmentID { get; set; }

        public AbstractAssignment AbstractAssignment { get; set; }

        /// <summary>
        /// Grading points possible relative to sibling assignment activities in the parent assignment.
        /// </summary>
        public int PointsPossible { get; set; }

        /// <summary>
        /// Used for visual ordering of assignment activities in the gradebook.  Defaults to a value of 0.
        /// </summary>
        public int ColumnOrder { get; set; }

        public virtual ICollection<Score> Scores { get; set; }

    }
}