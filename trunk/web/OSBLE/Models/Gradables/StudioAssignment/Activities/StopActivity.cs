using System;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Gradables.StudioAssignment.Activities
{
    public class StopActivity : IAssignmentActivity
    {
        public StopActivity()
        {
            DateTime dateTimeNow = DateTime.Now;

            //The default is a week from today a second before midnight
            ReleaseDate = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            ReleaseDate = ReleaseDate.AddDays(7);
        }

        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Release Date")]
        public DateTime ReleaseDate { get; set; }
    }
}