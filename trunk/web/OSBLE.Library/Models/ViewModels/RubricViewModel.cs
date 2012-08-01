using System.Collections.Generic;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses.Rubrics;

namespace OSBLE.Models.ViewModels
{
    public class RubricViewModel
    {
        public RubricViewModel()
        {
            Rubric = new Rubric();
            Evaluation = new RubricEvaluation();
            SelectedSection = 0;
            Completed = false;
            isMerged = false;
        }

        public Rubric Rubric { get; set; }

        public RubricEvaluation Evaluation { get; set; }

        public Assignment SelectedAssignment { get; set; }

        public AssignmentTeam SelectedTeam { get; set; }

        public List<Assignment> AssignmentList { get; set; }

        public List<AssignmentTeam> TeamList { get; set; }

        public List<RubricEvaluation> RubricEvaluationList { get; set; }

        public SelectList Sections { get; set; }

        public int SelectedSection { get; set; }

        public bool Student { get; set; }

        public bool Completed { get; set; }

        public bool isMerged { get; set; }
    }
}