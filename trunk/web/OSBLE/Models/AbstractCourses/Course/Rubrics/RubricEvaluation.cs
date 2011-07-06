﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;
using OSBLE.Models.Assignments;
namespace OSBLE.Models.Courses.Rubrics
{
    public class RubricEvaluation
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public int EvaluatorID { get; set; }
        public virtual UserProfile Evaluator { get; set; }

        [Required]
        public int RecipientID { get; set; }
        public virtual UserProfile Recipient { get; set; }

        [Required]
        public int AssignmentID { get; set; }
        public virtual AbstractAssignment Assignment { get; set; }

        [Required]
        public bool IsPublished { get; set; }

        public DateTime? DatePublished { get; set; }
    }
}