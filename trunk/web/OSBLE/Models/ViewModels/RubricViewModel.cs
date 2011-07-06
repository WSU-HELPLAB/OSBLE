using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses.Rubrics;

namespace OSBLE.Models.ViewModels
{
    public class RubricViewModel
    {
        public RubricViewModel()
        {
            Rubric = new Rubric();
            Evaluation = new RubricEvaluation();
        }

        public Rubric Rubric { get; set; }
        public RubricEvaluation Evaluation { get; set; }
    }
}