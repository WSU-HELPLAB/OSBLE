﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ServiceModel.DomainServices.Server;
using System.ComponentModel;

namespace OSBLE.Models.Courses.Rubrics
{
    public class Rubric
    {
        [Required]
        [Key]
        [Editable(true)]
        public int ID { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public bool HasCriteriaComments { get; set; }

        [Required]
        public bool HasGlobalComments { get; set; }

        [Association("Levels", "ID", "RubricID")]
        [Include]
        public virtual ICollection<Level> Levels { get; set; }

        [Association("Criteria", "ID", "RubricID")]
        [Include]
        public virtual ICollection<Criterion> Criteria { get; set; }

        public virtual ICollection<CellDescription> CellDescriptions { get; set; }
    }
}