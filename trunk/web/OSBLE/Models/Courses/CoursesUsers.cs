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
        public int CourseID { get; set; }

        public virtual AbstractCourse Course { get; set; }

        [Required]
        public int CourseRoleID { get; set; }

        public virtual AbstractRole CourseRole { get; set; }

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