﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.AbstractCourses
{
    public class CommentCategory
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public IList<CommentCategoryOption> Options { get; set; }

        public CommentCategory()
        {
            Options = new List<CommentCategoryOption>();
        }
    }
}