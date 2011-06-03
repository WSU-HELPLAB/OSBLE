﻿using System;
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

        public AssignmentActivity()
        {
            DateTime dateTimeNow = DateTime.Now;

            //The default is tomorrow at midnight
            ReleaseDate = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 0, 0, 0);
            ReleaseDate = ReleaseDate.AddDays(1);
        }

        [Required]
        [Display(Name = "Will Be Graded?")]
        public bool isGradeable { get; set; }

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

        public List<Score> Scores { get; set; }

    }
}