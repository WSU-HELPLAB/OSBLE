using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Assignments;
using OSBLE.Models.Users;
using System.Web.Mvc;
using OSBLE.Models.Assignments.Activities;

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
        public Assignment SelectedAssignment { get; set; }
        public AssignmentTeam SelectedTeam { get; set; }
        public List<Assignment> AssignmentList { get; set; }
        public List<AssignmentTeam> TeamList { get; set; }
        public SelectList Sections { get; set; }
        public int SelectedSection { get; set; }
    }
}