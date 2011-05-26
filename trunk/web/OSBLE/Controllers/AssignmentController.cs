using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    [Authorize]
    [ActiveCourseAttribute]
    [NotForCommunity]
    public class AssignmentController : OSBLEController
    {
        //
        // GET: /Assignment/

        public ViewResult Index()
        {
            var abstractgradables = db.AbstractGradables.Include(a => a.Weight);
            return View(abstractgradables.ToList());
        }

        //
        // GET: /Assignment/Details/5

        public ViewResult Details(int id)
        {
            Assignment assignment = db.AbstractGradables.Find(id) as Assignment;
            return View(assignment);
        }

        //
        // GET: /Assignment/Create

        public ActionResult Create()
        {
            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name");
            ViewBag.bobby = new SelectList(db.Deliverables, "ID", "Name");
            return View();
        }

        //
        // POST: /Assignment/Create

        [HttpPost]
        public ActionResult Create(Assignment assignment)
        {
            assignment.Deliverables = new List<Deliverable>();
            if (ModelState.IsValid)
            {
                db.AbstractGradables.Add(assignment);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name", assignment.WeightID);
            return View(assignment);
        }

        //
        // GET: /Assignment/Edit/5

        public ActionResult Edit(int id)
        {
            Assignment assignment = db.AbstractGradables.Find(id) as Assignment;
            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name", assignment.WeightID);
            return View(assignment);
        }

        //
        // POST: /Assignment/Edit/5

        [HttpPost]
        public ActionResult Edit(Assignment assignment)
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
            Assignment assignment = db.AbstractGradables.Find(id) as Assignment;
            return View(assignment);
        }

        //
        // POST: /Assignment/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Assignment assignment = db.AbstractGradables.Find(id) as Assignment;
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