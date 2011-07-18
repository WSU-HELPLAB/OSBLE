﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.AbstractCourses
{
    public class CommentCategoryConfiguration
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }

        public virtual ICollection<CommentCategory> Categories { get; set; }

        public CommentCategoryConfiguration()
        {
            Categories = new List<CommentCategory>();
        }
    }
}