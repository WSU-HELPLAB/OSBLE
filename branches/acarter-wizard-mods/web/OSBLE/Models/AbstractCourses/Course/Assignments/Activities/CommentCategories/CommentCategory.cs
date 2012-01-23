using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.AbstractCourses.Course.Assignments.Activities.CommentCategories
{
    public class CommentCategory
    {

        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<CommentCategoryTag> CommentCategoryTags { get; set; }

        public CommentCategory()
        {
            if (CommentCategoryTags == null)
            {
                CommentCategoryTags = new List<CommentCategoryTag>();
            }
        }

    }
}
