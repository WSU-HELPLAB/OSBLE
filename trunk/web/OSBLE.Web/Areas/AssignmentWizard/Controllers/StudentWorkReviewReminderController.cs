using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class StudentWorkReviewReminderController : WizardBaseController
    {

        public override string PrettyName
        {
            get { return "Student Work Review Settings"; }
        }

        public override string ControllerName
        {
            get { return "StudentWorkReviewSettings"; }
        }

        public override string ControllerDescription
        {
            get
            {
                return "More detailed information about the current student work review settings.";
            }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                return new StudentWorkReviewSettingsController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                List<AssignmentTypes> Assessments = new List<AssignmentTypes>();
                Assessments.Add(AssignmentTypes.ReviewOfStudentWork);
                return Assessments;
            }
        }

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
            Assignment.Type = manager.ActiveAssignmentType;
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = model;
            return base.PostBack(Assignment);
        }
    }
}
