﻿using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public class SubmissionActivity : StudioActivity
    {
        public SubmissionActivity()
            : base()
        {
            
        }

        [Required]
        [Display(Name = "Enable inline comments")]
        public bool InstructorCanReview { get; set; }

        public SubmissionActivity ShallowCopy()
        {
            return this.MemberwiseClone() as SubmissionActivity;
        }
    }
}