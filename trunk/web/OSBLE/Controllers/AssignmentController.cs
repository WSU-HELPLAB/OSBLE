using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.Gradables;
using OSBLE.Models.Gradables.StudioAssignment;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class AssignmentController : OSBLEController
    {
        public AssignmentController()
            : base()
        {
            ViewBag.CurrentTab = "Assignments";
        }
        //
        // GET: /Assignment/

        public ViewResult Index()
        {
            //This is what it was but no longer works
            var abstractgradables = db.AbstractGradables.ToList();
            return View(abstractgradables);
            //return View();
        }

        //
        // GET: /Assignment/Details/5

        public ViewResult Details(int id)
        {
            SubmissionActivitySettings assignment = db.AbstractGradables.Find(id) as SubmissionActivitySettings;
            return View(assignment);
        }

        //
        // GET: /Assignment/Create

        public ActionResult Create()
        {
            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name");
            ViewBag.Deliverable = new SelectList(db.Deliverables, "ID", "Name");
            return View(new SubmissionActivitySettings());
        }

        //
        // POST: /Assignment/Create

        [HttpPost]
        public ActionResult Create(SubmissionActivitySettings assignment)
        {
            assignment.Deliverables = new List<Deliverable>();

            if (assignment.ReleaseDate >= assignment.DueDate)
            {
                ModelState.AddModelError("time", "The due date must come after the release date");
            }

            if (ModelState.IsValid)
            {
                db.AbstractGradables.Add(assignment);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name", assignment.WeightID);
            ViewBag.Deliverable = new SelectList(db.Deliverables, "ID", "Name");
            return View(assignment);
        }

        //
        // GET: /Assignment/Edit/5

        public ActionResult Edit(int id)
        {
            SubmissionActivitySettings assignment = db.AbstractGradables.Find(id) as SubmissionActivitySettings;
            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name", assignment.WeightID);
            return View(assignment);
        }

        //
        // POST: /Assignment/Edit/5

        [HttpPost]
        public ActionResult Edit(SubmissionActivitySettings assignment)
        {
            if (ModelState.IsValid)
            {
                db.Entry(assignment).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name", assignment.WeightID);
            return View(assignment);
        }

        //
        // GET: /Assignment/Delete/5

        public ActionResult Delete(int id)
        {
            SubmissionActivitySettings assignment = db.AbstractGradables.Find(id) as SubmissionActivitySettings;
            return View(assignment);
        }

        //
        // POST: /Assignment/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SubmissionActivitySettings assignment = db.AbstractGradables.Find(id) as SubmissionActivitySettings;
            db.AbstractGradables.Remove(assignment);
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