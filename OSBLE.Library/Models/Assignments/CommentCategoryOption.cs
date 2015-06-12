using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments
{
    public class CommentCategoryOption
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required (ErrorMessage="The category option must have a name")]
        public string Name { get; set; }
    }
}