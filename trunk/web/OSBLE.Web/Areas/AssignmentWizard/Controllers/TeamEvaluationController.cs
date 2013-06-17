using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using System.Web.Mvc;
using OSBLE.Areas.AssignmentWizard.Models;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class TeamEvaluationController : WizardBaseController
    {
#region WizardBaseController Overrides
        public override string ControllerName
        {
            get { return "TeamEvaluation"; }
        }

        public override string ControllerDescription
        {
            get { return "This assignment contains a team evaluation."; }
        }

        public override IWizardBaseController Prerequisite
        {
            get
            {
                return new PreviousAssignmentController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.TeamEvaluation }).ToList();
            }
        }

        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        public override string PrettyName
        {
            get
            {
                return "Evaluation Settings";
            }
        }
#endregion

        public override ActionResult Index()
        {
            base.Index();
            if (Assignment.TeamEvaluationSettings == null)
            {
                Assignment.TeamEvaluationSettings = new TeamEvaluationSettings();
                Assignment.TeamEvaluationSettings.DiscrepancyCheckSize = 0;
                Assignment.TeamEvaluationSettings.RequiredCommentLength = 0;
                Assignment.TeamEvaluationSettings.MaximumMultiplier = 1.35;
                Assignment.TeamEvaluationSettings.AssignmentID = Assignment.ID;
            }
            return View(Assignment.TeamEvaluationSettings);
        }

        [HttpPost]
        public ActionResult Index(TeamEvaluationSettings model)
        {
            Assignment = db.Assignments.Find(model.AssignmentID);
            if (ModelState.IsValid)
            {
                //delete preexisting settings to prevent an FK relation issue
                TeamEvaluationSettings setting = db.TeamEvaluationSettings.Find(model.AssignmentID);
                if (setting != null)
                {
                    db.TeamEvaluationSettings.Remove(setting);
                }
                db.SaveChanges();

                //...and then re-add it.
                Assignment.TeamEvaluationSettings = model;
                db.SaveChanges();
                WasUpdateSuccessful = true;
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            return base.PostBack(Assignment.TeamEvaluationSettings);
        }
    }
}