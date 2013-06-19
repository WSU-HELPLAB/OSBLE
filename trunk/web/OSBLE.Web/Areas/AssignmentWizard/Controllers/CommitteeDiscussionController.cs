using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CommitteeDiscussionController : WizardBaseController
    {

        public override string PrettyName
        {
            get { return "Committee Discussion"; }
        }

        public override string ControllerName
        {
            get { return "CommitteeDiscussion"; }
        }

        public override string ControllerDescription
        {
            get
            {
                return "More detailed information about a committee discussion.";
            }
        }

        public override WizardBaseController Prerequisite
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
                Assessments.Add(AssignmentTypes.CommitteeDiscussion);
                Assessments.Add(AssignmentTypes.CommitteeDiscussionOfStudentWorkReview);
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
