using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.AbstractCourses
{
    public class CommentCategoryOption
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }
    }
}