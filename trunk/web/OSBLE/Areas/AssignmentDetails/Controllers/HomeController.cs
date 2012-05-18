using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentDetails.ViewModels;

namespace OSBLE.Areas.AssignmentDetails.Controllers
{
    public class HomeController : OSBLEController
    {
        public ActionResult Index(int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            AssignmentDetailsFactory factory = new AssignmentDetailsFactory();
            AssignmentDetailsViewModel viewModel = factory.Bake(assignment, ActiveCourse);

            //discussion assignments require their own special view
            if (assignment.Type != AssignmentTypes.DiscussionAssignment)
            {
                return View(viewModel);
            }
            else
            {
                return View("DiscussionAssignment", viewModel);
            }
        }
    }
}
