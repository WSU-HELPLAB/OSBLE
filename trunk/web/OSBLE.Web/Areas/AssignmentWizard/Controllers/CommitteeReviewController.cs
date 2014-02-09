using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CommitteeReviewController : WizardBaseController
    {
        public override string PrettyName
        {
            get { return "Committee Review"; }
        }

        public override string ControllerName
        {
            get { return "CommitteeReview"; }
        }

        public override string ControllerDescription
        {
            get
            {
                return "Specific information on the committee review being created";
            }
        }

        public override IWizardBaseController Prerequisite
        {
            get
            {
                return new AssessmentBasicsController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                List<AssignmentTypes> Assessments = new List<AssignmentTypes>();
                //Assessments.Add(AssignmentTypes.CommitteeReview);
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
            ViewBag.TemporaryID = Assignment.ID;
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = db.Assignments.Find(model.ID);
            return base.PostBack(Assignment);
        }
    }
}