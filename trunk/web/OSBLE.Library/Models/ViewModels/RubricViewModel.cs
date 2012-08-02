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
            _MergedEvaluations = new List<RubricEvaluation>();
            Evaluation = new RubricEvaluation();
            SelectedSection = 0;
            Completed = false;
            isMerged = false;
        }

        public Rubric Rubric { get; set; }

        public RubricEvaluation Evaluation { get; set; }

        private List<RubricEvaluation> _MergedEvaluations;

        /// <summary>
        /// </summary>
        public List<RubricEvaluation> MergedEvaluations
        {
            get
            {
                if (isMerged == false && _MergedEvaluations.Count < 1 && Evaluation != null)
                {
                    _MergedEvaluations.Add(Evaluation);
                }
                return _MergedEvaluations;
            }
            set
            {
                _MergedEvaluations = value;
            }
        }

        public Assignment SelectedAssignment { get; set; }

        public AssignmentTeam SelectedTeam { get; set; }

        public List<Assignment> AssignmentList { get; set; }

        public List<AssignmentTeam> TeamList { get; set; }

        /// <summary>
        /// Used for the drop down selection, to switch to another rubric evalutaion
        /// </summary>
        public List<RubricEvaluation> RubricEvaluationList { get; set; }

        public SelectList Sections { get; set; }

        public int SelectedSection { get; set; }


        /// <summary>
        /// if true, then this viewModel represents a student rubric
        /// </summary>
        public bool Student { get; set; }

        /// <summary>
        /// if true, then this view model has been created from an existing rubric evaluation.
        /// otherwise, the viewmodel is created using default values.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Used to determine if this view model represents merged rubrics
        /// </summary>
        public bool isMerged { get; set; }
    }
}