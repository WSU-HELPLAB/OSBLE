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
        public AbstractAssignmentActivity SelectedAssignmentActivity { get; set; }
        public List<AbstractAssignmentActivity> AssignmentActivities { get; set; }
        public List<TeamUserMember> TeamUsers { get; set; }
        public TeamUserMember SelectedTeam { get; set; }
        public SelectList Sections { get; set; }
        public int SelectedSection { get; set; }
    }
}