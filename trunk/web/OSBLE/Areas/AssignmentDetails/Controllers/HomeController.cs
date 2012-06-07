using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentDetails.ViewModels;
using OSBLE.Attributes;

namespace OSBLE.Areas.AssignmentDetails.Controllers
{
    public class HomeController : OSBLEController
    {
        public ActionResult Index(int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            if (assignment == null)
            {
                return RedirectToRoute(new { action = "Index", controller = "Assignment", area = "" });
            }
            AssignmentDetailsFactory factory = new AssignmentDetailsFactory();
            AssignmentDetailsViewModel viewModel = factory.Bake(assignment, ActiveCourseUser);

            //discussion assignments and critical reviews require their own special view
            if (assignment.Type == AssignmentTypes.CriticalReview)
            {
                return View("CriticalReviewAssignment", viewModel);
            }
            else if (assignment.Type == AssignmentTypes.DiscussionAssignment ||
                    assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
            {
                return View("DiscussionAssignment", viewModel);
            }
            else
            {
                return View("Index", viewModel);
            }
        }

        [CanModifyCourse]
        public ActionResult ToggleDraft(int assignmentId)
        {
            Assignment.ToggleDraft(assignmentId, ActiveCourseUser.ID);
            return Index(assignmentId);
        }

        [CanModifyCourse]
        public ActionResult DeleteAssignment(int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            db.Assignments.Remove(assignment);
            db.SaveChanges();
            return RedirectToRoute(new { action = "Index", controller = "Assignment", area = "" });
        }
    }
}
