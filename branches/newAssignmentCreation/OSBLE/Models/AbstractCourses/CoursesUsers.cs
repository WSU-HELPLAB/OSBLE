using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;

namespace OSBLE.Models.Courses
{
    public class CoursesUsers
    {
        [Required]
        [Key]
        [Column(Order = 0)]
        public int UserProfileID { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        [Required]
        [Key]
        [Column(Order = 1)]
        public int AbstractCourseID { get; set; }

        public virtual AbstractCourse AbstractCourse { get; set; }

        [Required]
        public int AbstractRoleID { get; set; }

        public virtual AbstractRole AbstractRole { get; set; }

        [Required]
        public int Section { get; set; }

        public bool Hidden { get; set; }

        public CoursesUsers()
            : base()
        {
            Hidden = false;
        }
    }
}