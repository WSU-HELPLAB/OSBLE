using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Gradables.StudioAssignment
{
    public class SubmissionActivitySettings : AbstractGradable
    {
        public SubmissionActivitySettings()
            : base()
        {
            DateTime dateTimeNow = DateTime.Now;

            //The default is tomorrow at midnight
            ReleaseDate = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 0, 0, 0);
            ReleaseDate = ReleaseDate.AddDays(1);

            //The default time is 1 week from the ReleaseDate and it is due time is right before the next day 11:59:59.999
            DueDate = new DateTime(ReleaseDate.Year, ReleaseDate.Month, ReleaseDate.Day, 23, 59, 59);
            DueDate = DueDate.AddDays(7);

            isGradeable = true;
        }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Release Date")]
        public DateTime ReleaseDate { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Required]

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Deliverables")]
        public virtual ICollection<Deliverable> Deliverables { get; set; }

        [Required]
        [Display(Name = "Will Be Graded?")]
        public bool isGradeable { get; set; }

        [Required]
        [Display(Name = "Is Team")]
        public bool isTeam { get; set; }

        [Required]
        [Display(Name = "Can Instructor Do A Line By Line Review?")]
        public bool InstructorCanReview { get; set; }

        //NEED RUBRIC
    }
}