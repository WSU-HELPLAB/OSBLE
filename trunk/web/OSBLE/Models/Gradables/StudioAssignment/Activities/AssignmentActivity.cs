using System.ComponentModel.DataAnnotations;
using System;

namespace OSBLE.Models.Gradables.StudioAssignment
{
    public abstract class AssignmentActivity : AbstractGradable
    {
        public AssignmentActivity()
        {
            DateTime dateTimeNow = DateTime.Now;

            //The default is tomorrow at midnight
            ReleaseDate = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 0, 0, 0);
            ReleaseDate = ReleaseDate.AddDays(1);
        }

        [Required]
        public int AssignmentID { get; set; }


        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Release Date")]
        public DateTime ReleaseDate { get; set; }

        [Required]
        [Display(Name = "Will Be Graded?")]
        public bool isGradeable { get; set; }
    }
}