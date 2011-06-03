using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Models.Assignments
{
    public class BasicAssignment : AbstractAssignment, ILatePolicy
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

        public SubmissionActivity GetSubmissionActivity()
        {
            return (SubmissionActivity)AssignmentActivities.Where(aa => aa is SubmissionActivity).FirstOrDefault();
        }

        public StopActivity GetStopActivity()
        {
            return (StopActivity)AssignmentActivities.Where(aa => aa is StopActivity).FirstOrDefault();
        }

        public BasicAssignment() : base() { 

            // Check for Submission Activity and Stop Activity and create them if they don't exist.
            if (AssignmentActivities.Where(aa => aa is SubmissionActivity).FirstOrDefault() == null)
            {
                SubmissionActivity submissionActivity = new SubmissionActivity();
                AssignmentActivities.Add(submissionActivity);
            }

            if (AssignmentActivities.Where(aa => aa is StopActivity).FirstOrDefault() == null)
            {
                StopActivity stopActivity = new StopActivity();
                AssignmentActivities.Add(stopActivity);
            }

        }
    }
}