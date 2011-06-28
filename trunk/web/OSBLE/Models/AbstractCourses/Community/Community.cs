using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses
{
    public class Community : AbstractCourse
    {
        // Community Options

        [Display(Name = "Community Description")]
        [StringLength(100)]
        [Required]
        public string Description { get; set; }

        [Display(Name = "Enter a short (3-4 character) nickname to display in the dashboard for this community (e.g. Comm)")]
        [StringLength(10)]
        [Required]
        public string Nickname { get; set; }

        [Display(Name = "Allow all community members to post events in calendar")]
        public override bool AllowEventPosting { get; set; }

        [Display(Name = "Allow all community members to upload files")]
        public bool AllowFileUpload { get; set; }

        public Community()
            : base()
        {
            AllowEventPosting = true;
        }
    }
}