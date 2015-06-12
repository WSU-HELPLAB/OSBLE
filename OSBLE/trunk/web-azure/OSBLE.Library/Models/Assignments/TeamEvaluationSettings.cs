﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class TeamEvaluationSettings : IModelBuilderExtender
    {
        [Key]
        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        /// <summary>
        /// The maximum allowed multiplier that can be used when team evaluations impact a student's
        /// grade.  Ex: after student evaluations, Stu has a grade of 150%.  If the maximum
        /// multiplier was 1.35, then Stu's grade would get chopped to 135%.
        /// </summary>
        [Display(Name="Maximum multiplier allowed per student")]
        [Required(AllowEmptyStrings=true, ErrorMessage="Please enter a multiplier.")]
        public double MaximumMultiplier { get; set; }

        /// <summary>
        /// If <see cref="DiscrepancyCheckSize"/> is set and a student's review of another student
        /// exceeds that amount, then require the evaluator to submit a comment of this lenght.
        /// Setting this to zero will ignore this setting.
        /// </summary>
        [Display(Name="Require an explanation of the specified length when a discrepancy occurs")]
        [Required(AllowEmptyStrings = true, ErrorMessage = "Please enter an explanation length (use 0 if not necessary).")]
        public int RequiredCommentLength { get; set; }

        /// <summary>
        /// If not zero, instructors will be notified if a set of evaluations from a student 
        /// has a maximum difference greater than the specified amount.
        /// Example: Stu giving Bob a 90%  and himself a 110% when this is set to 19 would set a flag.
        /// </summary>
        [Display(Name = "Notify instructor if a student performs an evaluation with a percent spread larger than the following amount")]
        [Required(AllowEmptyStrings = true, ErrorMessage = "Please enter a notification amount (use 0 if not necessary).")]
        public int DiscrepancyCheckSize { get; set; }

        public TeamEvaluationSettings()
        {
        }

        public TeamEvaluationSettings(TeamEvaluationSettings other)
            : this()
        {
            if (other == null)
            {
                return;
            }
            this.AssignmentID = other.AssignmentID;
            this.DiscrepancyCheckSize = other.DiscrepancyCheckSize;
            this.MaximumMultiplier = other.MaximumMultiplier;
            this.RequiredCommentLength = other.RequiredCommentLength;
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamEvaluationSettings>()
                .HasRequired(tes => tes.Assignment)
                .WithOptional(a => a.TeamEvaluationSettings)
                .WillCascadeOnDelete(true);
        }
    }
}