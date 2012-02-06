﻿using System;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;
using System.Linq;

namespace OSBLE.Models.Assignments
{
    public class Score
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int CourseUserID { get; set; }
        public virtual CourseUser CourseUser { get; set; }

        [Required]
        public int TeamID { get; set; }
        public virtual Team Team { get; set; }

        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        [Required]
        [Display(Name = "Grade Is Published")]
        public bool Published { get; set; }

        [Required]
        public DateTime? PublishedDate { get; set; }

        [Required]
        public double Points { get; set; }

        [Required]
        public double Multiplier { get; set; }

        [Required]
        public double LatePenaltyPercent { get; set; }

        [Required]
        public double CustomLatePenaltyPercent { get; set; }

        [Required]
        public bool isDropped { get; set; }

        public double StudentPoints { get; set; }

        public double AddedPoints { get; set; }

        public double RawPoints { get; set; }

        public Score()
        {
            Published = false;
            Points = 0;
            Multiplier = 1.0;
            LatePenaltyPercent = 0;
            StudentPoints = -1;
            AddedPoints = 0;
            CustomLatePenaltyPercent = -1;
            isDropped = false;
            RawPoints = -1;
        }

        /// <summary>
        /// Returns the currently applied late penalty. If the CustomLatePenalty is >0, then CustomLatePenalty is the late penalty. Else it is always LatePenaltyPercent
        /// </summary>
        /// <returns></returns>
        public double getAppliedLatePenaltyAsDecimal()
        {
            if (this.CustomLatePenaltyPercent >= 0)
                return this.CustomLatePenaltyPercent / 100.0;
            else
                return this.LatePenaltyPercent / 100.0;
        }

        /// <summary>
        /// Returns the grade percentage as a string, or as "NG" if there is no grade(points == -1) or points possible is 0
        /// </summary>
        /// <returns></returns>
        public string getGradeAsPercent(int assignmentPossiblePoints)
        {
            if (this.Points == -1 || assignmentPossiblePoints == 0)
                return "NG";
            else
                return ((this.Points / (double)assignmentPossiblePoints)).ToString("P");
        }
    }
}