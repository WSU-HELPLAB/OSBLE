using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Areas.AssignmentWizard.Models;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class PreviousAssignmentController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "PreviousAssignment"; }
        }

        public override string ControllerDescription
        {
            get { return "This assignment depends on a previous assignment."; }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                prereqs.Add(new BasicsController());
                prereqs.Add(new TeamController());
                return prereqs;
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get 
            {
                return (new AssignmentTypes[] { AssignmentTypes.PeerReview }).ToList();
            }
        }

        public ActionResult Index()
        {
            return View();
        }
    }
}
