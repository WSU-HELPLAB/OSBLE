using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Assignments;
using OSBLE.Models.Users;
using System.Web.Mvc;

namespace OSBLE.Models.ViewModels
{
    public class RubricViewModel
    {
        public RubricViewModel()
        {
            Rubric = new Rubric();
            Evaluation = new RubricEvaluation();
            SelectedSection = 0;
        }

        public Rubric Rubric { get; set; }
        public RubricEvaluation Evaluation { get; set; }
        public AbstractAssignment SelectedAssignment { get; set; }
        public SelectList Assignments { get; set; }
        public SelectList Students { get; set; }
        public UserProfile SelectedStudent { get; set; }
        public SelectList Sections { get; set; }
        public int SelectedSection { get; set; }
    }
}