using System;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;

namespace OSBLE.Models.Assignments.Activities.Scores
{
    public class Score
    {
        [Required]
        [Key]
        public int ID { get; set; }

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

        public double ManualLatePenaltyPercent { get; set; }

        [Required]
        public int TeamUserMemberID { get; set; }

        public virtual TeamUserMember TeamUserMember { get; set; }

        [Required]
        public int AssignmentActivityID { get; set; }

        [Required]
        public bool isDropped { get; set; }

        public virtual AbstractAssignmentActivity AssignmentActivity { get; set; }

        public double StudentPoints { get; set; }

        public double AddedPoints { get; set; }

        public Score()
        {
            Published = false;
            Points = 0;
            Multiplier = 1.0;
            LatePenaltyPercent = 0;
            StudentPoints = -1;
            AddedPoints = 0;
            //isTake = false;
            isDropped = false;
            ManualLatePenaltyPercent = -1;
        }
    }
}