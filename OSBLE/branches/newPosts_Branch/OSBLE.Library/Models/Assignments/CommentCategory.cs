using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class CommentCategory
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required(ErrorMessage="The comment category must have a name")]
        public string Name { get; set; }

        [Required]
        public virtual IList<CommentCategoryOption> Options { get; set; }

        public CommentCategory()
        {
            Options = new List<CommentCategoryOption>();
        }
    }
}