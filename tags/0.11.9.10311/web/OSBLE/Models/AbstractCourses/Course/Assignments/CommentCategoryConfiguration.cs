using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.AbstractCourses.Course
{
    public class CommentCategoryConfiguration
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage="The comment category configuration must have a name")]
        public string Name { get; set; }

        public virtual IList<CommentCategory> Categories { get; set; }

        public CommentCategoryConfiguration()
        {
            Categories = new List<CommentCategory>();
        }
    }
}