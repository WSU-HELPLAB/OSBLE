using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CriticalReviewSettingsController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "CriticalReviewSettings"; }
        }

        public override string PrettyName
        {
            get
            {
                return "Critical Review Settings";
            }
        }

        public override string ControllerDescription
        {
            get { return "Settings for critical review assignment type."; }
        }

        public override WizardBaseController Prerequisite
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
                return (new AssignmentTypes[] { AssignmentTypes.CriticalReview }).ToList();
            }
        }

        /// <summary>
        /// The discussion component is required for critical review assignments
        /// </summary>
        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        public override ActionResult Index()
        {
            base.Index();
            if (Assignment.CriticalReviewSettings == null)
            {
                Assignment.CriticalReviewSettings = new CriticalReviewSettings() { AssignmentID = Assignment.ID };
            }
            return View(Assignment.CriticalReviewSettings);
        }

        [HttpPost]
        public ActionResult Index(CriticalReviewSettings model)
        {
            Assignment = db.Assignments.Find(model.AssignmentID);
            if (ModelState.IsValid)
            {
                //delete preexisting settings to prevent an FK relation issue
                CriticalReviewSettings setting = db.CriticalReviewSettings.Find(model.AssignmentID);
                if (setting != null)
                {
                    db.CriticalReviewSettings.Remove(setting);
                }
                db.SaveChanges();

                //...and then re-add it.
                Assignment.CriticalReviewSettings = model;
                db.SaveChanges();
                WasUpdateSuccessful = true;
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            return base.PostBack(Assignment.CriticalReviewSettings);
        }
    }
}
