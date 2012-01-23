using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Controllers
{
    public class AuthorRebuttalController : OSBLEController
    {
        //
        // GET: /AuthorRebuttal/

        public ViewResult Index()
        {
            var assignmentactivities = db.AbstractAssignmentActivities.Include(a => a.AbstractAssignment);
            return View(assignmentactivities.ToList());
        }

        //
        // GET: /AuthorRebuttal/Details/5

        public ViewResult Details(int id)
        {
            AuthorRebuttalActivity authorrebuttalactivity = db.AbstractAssignmentActivities.Find(id) as AuthorRebuttalActivity;
            return View(authorrebuttalactivity);
        }

        //
        // GET: /AuthorRebuttal/Create

        public ActionResult Create()
        {
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name");
            return View();
        }

        //
        // POST: /AuthorRebuttal/Create

        [HttpPost]
        public ActionResult Create(AuthorRebuttalActivity authorrebuttalactivity)
        {
            string presentation = Request.Params["Presentation"];

            // had to use hard coded strings because otherwise through an error about constant values.
            switch (presentation)
            {
                case "PresentAllIssuesLogged":
                    authorrebuttalactivity.Presentation = AuthorRebuttalActivity.PresentationOptions.PresentAllIssuesLogged;
                    break;
                case "PresentIssuesXLogged":
                    authorrebuttalactivity.Presentation = AuthorRebuttalActivity.PresentationOptions.PresentIssuesXLogged;
                    break;
                case "PresentIssuesXPercentLogged":
                    authorrebuttalactivity.Presentation = AuthorRebuttalActivity.PresentationOptions.PresentIssuesXPercentLogged;
                    break;
                case "PresentOnlyIssuesModeratorVoted":
                    authorrebuttalactivity.Presentation = AuthorRebuttalActivity.PresentationOptions.PresentOnlyModeratorVoted;
                    break;
                default:
                    break;
            };
            if (ModelState.IsValid)
            {
                db.AbstractAssignmentActivities.Add(authorrebuttalactivity);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", authorrebuttalactivity.AbstractAssignmentID);
            return View(authorrebuttalactivity);
        }

        //
        // GET: /AuthorRebuttal/Edit/5

        public ActionResult Edit(int id)
        {
            AuthorRebuttalActivity authorrebuttalactivity = db.AbstractAssignmentActivities.Find(id) as AuthorRebuttalActivity;
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", authorrebuttalactivity.AbstractAssignmentID);
            return View(authorrebuttalactivity);
        }

        //
        // POST: /AuthorRebuttal/Edit/5

        [HttpPost]
        public ActionResult Edit(AuthorRebuttalActivity authorrebuttalactivity)
        {
            if (ModelState.IsValid)
            {
                db.Entry(authorrebuttalactivity).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", authorrebuttalactivity.AbstractAssignmentID);
            return View(authorrebuttalactivity);
        }

        //
        // GET: /AuthorRebuttal/Delete/5

        public ActionResult Delete(int id)
        {
            AuthorRebuttalActivity authorrebuttalactivity = db.AbstractAssignmentActivities.Find(id) as AuthorRebuttalActivity;
            return View(authorrebuttalactivity);
        }

        //
        // POST: /AuthorRebuttal/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            AuthorRebuttalActivity authorrebuttalactivity = db.AbstractAssignmentActivities.Find(id) as AuthorRebuttalActivity;
            db.AbstractAssignmentActivities.Remove(authorrebuttalactivity);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}