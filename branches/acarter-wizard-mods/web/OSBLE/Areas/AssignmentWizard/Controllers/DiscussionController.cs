using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class DiscussionController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "Discussion"; }
        }

        public override string ControllerDescription
        {
            get { return "This assignment has students discuss one or more topics."; }
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
                return (new AssignmentTypes[] { AssignmentTypes.DiscussionAssignment }).ToList();
            }
        }

        /// <summary>
        /// The discussion component is required for discussion assignments
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
            if (Assignment.DiscussionSettings == null)
            {
                Assignment.DiscussionSettings = new DiscussionSetting();
                Assignment.DiscussionSettings.InitialPostDueDate = Assignment.ReleaseDate.Add(new TimeSpan(7, 0, 0, 0, 0));
                Assignment.DiscussionSettings.AssignmentID = Assignment.ID;
            }
            return View(Assignment.DiscussionSettings);
        }

        [HttpPost]
        public ActionResult Index(DiscussionSetting model)
        {
            Assignment = db.Assignments.Find(model.AssignmentID);
            Assignment.DiscussionSettings = model;
            if (ModelState.IsValid)
            {
                WasUpdateSuccessful = true;
                db.SaveChanges();
            }
            return base.Index(Assignment);
        }
    }
}
