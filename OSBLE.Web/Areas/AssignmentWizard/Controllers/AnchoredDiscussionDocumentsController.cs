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
    public class AnchoredDiscussionDocumentsController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "AnchoredDiscussionDocuments"; }
        }

        public override string PrettyName
        {
            get
            {
                return "Select Anchored Discussion Documents";
            }
        }

        public override string ControllerDescription
        {
            get { return "This assignment requires that instructors submit one or more files for an Anchored Discussion."; }
        }

        public override IWizardBaseController Prerequisite
        {
            get
            {
                //
                return new BasicsController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get 
            {
                return (new AssignmentTypes[] { AssignmentTypes.AnchoredDiscussion }).ToList();
            }
        }

        /// <summary>
        /// The previous assignment component is required for all assignments listed in ValidAssignmentTypes
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
            ModelState.Clear();
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = db.Assignments.Find(model.ID);
            WasUpdateSuccessful = true;
            return base.PostBack(model);
        }
    }
}
