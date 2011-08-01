using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities.Scores;
using OSBLE.Models.Users;

namespace OSBLE.Models.Assignments.Activities
{
    public abstract class AbstractAssignmentActivity
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        public AbstractAssignmentActivity()
        {
            DateTime dateTimeNow = DateTime.Now;

            ColumnOrder = 0;

            //The default is tomorrow at midnight
            ReleaseDate = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 0, 0, 0);
            ReleaseDate = ReleaseDate.AddDays(1);

            TeamUsers = new List<TeamUserMember>();

            addedPoints = 0;
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

        public virtual AbstractAssignment AbstractAssignment { get; set; }

        /// <summary>
        /// Grading points possible relative to sibling assignment activities in the parent assignment.
        /// </summary>
        public int PointsPossible { get; set; }

        /// <summary>
        /// Used for visual ordering of assignment activities in the gradebook.  Defaults to a value of 0.
        /// </summary>
        public int ColumnOrder { get; set; }

        public double addedPoints { get; set; }

        public virtual ICollection<Score> Scores { get; set; }

        [Required]
        [Display(Name = "Is This A Team Activity?")]
        public bool isTeam { get; set; }

        public virtual ICollection<TeamUserMember> TeamUsers { get; set; }

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

        public AbstractAssignment ShallowCopy()
        {
            return this.MemberwiseClone() as AbstractAssignment;
        }
    }
}