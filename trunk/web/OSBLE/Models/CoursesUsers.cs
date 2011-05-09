using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
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
        public virtual Course Course { get; set; }

        [Required]
        public int CourseRoleID { get; set; }
        public virtual CourseRole CourseRole { get; set; }

        [Required]
        public int Section { get; set; }
    }
}