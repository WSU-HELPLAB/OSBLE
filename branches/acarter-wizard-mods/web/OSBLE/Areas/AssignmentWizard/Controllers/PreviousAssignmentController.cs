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
                return (new AssignmentTypes[] { AssignmentTypes.CriticalReview }).ToList();
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
            List<Assignment> assignments = (from
                                             assignment in db.Assignments
                                            where
                                             assignment.Category.CourseID == activeCourse.AbstractCourseID
                                             &&
                                             assignment.ID != Assignment.ID //ignore the current assignment
                                            select assignment).ToList();
            ViewBag.PreviousAssignments = assignments;
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            int previousAssignmentId = 0;
            Int32.TryParse(Request.Form["PreviousAssignmentSelect"], out previousAssignmentId);
            if (previousAssignmentId > 0)
            {
                Assignment = db.Assignments.Find(model.ID);
                WasUpdateSuccessful = true;
                Assignment.PrecededingAssignmentID = previousAssignmentId;
                db.SaveChanges();
            }
            return base.Index(model);
        }
    }
}
