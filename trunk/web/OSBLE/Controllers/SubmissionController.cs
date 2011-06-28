using System;
using System.Web;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Controllers
{
    [Authorize]
    [CanSubmitAssignments]
    public class SubmissionController : OSBLEController
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /Submission/Create

        public ActionResult Create(int? id)
        {
            if (id != null)
            {
                AbstractAssignmentActivity activity = db.AbstractAssignmentActivity.Find(id);

                AbstractAssignment assignment = db.AbstractAssignments.Find(activity.ID);

                if (activity != null && assignment.Category.CourseID == activeCourse.CourseID && activeCourse.CourseRole.CanSubmit == true && assignment is StudioAssignment)
                {
                    ViewBag.Deliverables = (assignment as StudioAssignment).Deliverables;
                    return View();
                }
            }

            throw new Exception();
        }

        //
        // POST: /Submission/Create

        [HttpPost]
        public ActionResult Create(HttpPostedFileBase submission)
        {
            // if (ModelState.IsValid)
            //{
            //  db.Submissions.Add(submission);
            //    db.SaveChanges();
            //    return RedirectToAction("Index");
            // }

            return View(submission);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}