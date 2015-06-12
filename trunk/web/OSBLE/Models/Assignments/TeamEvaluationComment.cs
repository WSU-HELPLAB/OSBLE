using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments
{
    public class TeamEvaluationComment
    {
        [Required]
        [Key]
        public int ID { get; set; }

        public string Comment { get; set; }

        public override string ToString()
        {
            return Comment;
        }
    }
}