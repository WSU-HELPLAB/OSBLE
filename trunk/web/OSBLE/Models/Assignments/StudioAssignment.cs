using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using OSBLE.Models.Assignments.Activities;
using System;

namespace OSBLE.Models.Assignments
{
    public class StudioAssignment : AbstractAssignment, ILatePolicy
    {
        // Late Policy

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

        public StudioAssignment()
        {

        }

    }
}