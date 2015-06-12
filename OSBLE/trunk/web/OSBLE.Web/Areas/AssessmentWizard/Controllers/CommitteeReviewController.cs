using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Assessments;

namespace OSBLE.Areas.AssessmentWizard.Controllers
{
    public class CommitteeReviewController : AssessmentBaseController
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

        public override ICollection<AssessmentType> ValidAssessmentTypes
        {
            get
            {
                List<AssessmentType> Assessments = new List<AssessmentType>();
                Assessments.Add(AssessmentType.CommitteeReview);
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
            Assessment.Type = manager.ActiveAssessmentType;
            return View(Assessment);
        }

        [HttpPost]
        public ActionResult Index(Assessment model)
        {
            Assessment = db.Assessments.Find(model.ID);
            return base.PostBack(model);
        }
    }
}