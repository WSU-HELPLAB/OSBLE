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

        public override string PrettyName
        {
            get
            {
                return "Discussion Settings";
            }
        }


        public override string ControllerDescription
        {
            get { return "This assignment has students discuss one or more topics."; }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                return new TeamController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.DiscussionAssignment, AssignmentTypes.CriticalReviewDiscussion }).ToList();
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
            if (ModelState.IsValid)
            {
                //delete preexisting settings to prevent an FK relation issue
                DiscussionSetting setting = db.DiscussionSettings.Find(model.AssignmentID);
                if (setting != null)
                {
                    db.DiscussionSettings.Remove(setting);
                }
                db.SaveChanges();

                //...and then re-add it.
                Assignment.DiscussionSettings = model;
                db.SaveChanges();
                WasUpdateSuccessful = true;
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            return base.PostBack(Assignment.DiscussionSettings);
        }
    }
}
