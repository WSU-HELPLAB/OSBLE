using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    public class AssignmentActivityController : OSBLEController
    {
        //
        // GET: /AssignmentActivity/

        public ViewResult Index()
        {
            return View(db.AssignmentActivities.ToList());
        }

        //
        // GET: /AssignmentActivity/Details/5

        public ViewResult Details(string id)
        {
            AssignmentActivity assignmentactivity = db.AssignmentActivities.Find(id);
            return View(assignmentactivity);
        }

        //
        // GET: /AssignmentActivity/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /AssignmentActivity/Create

        [HttpPost]
        public ActionResult Create(AssignmentActivity assignmentactivity)
        {
            if (ModelState.IsValid)
            {
                db.AssignmentActivities.Add(assignmentactivity);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(assignmentactivity);
        }

        //
        // GET: /AssignmentActivity/Edit/5

        public ActionResult Edit(string id)
        {
            AssignmentActivity assignmentactivity = db.AssignmentActivities.Find(id);
            return View(assignmentactivity);
        }

        //
        // POST: /AssignmentActivity/Edit/5

        [HttpPost]
        public ActionResult Edit(AssignmentActivity assignmentactivity)
        {
            if (ModelState.IsValid)
            {
                db.Entry(assignmentactivity).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(assignmentactivity);
        }

        //
        // GET: /AssignmentActivity/Delete/5

        public ActionResult Delete(string id)
        {
            AssignmentActivity assignmentactivity = db.AssignmentActivities.Find(id);
            return View(assignmentactivity);
        }

        //
        // POST: /AssignmentActivity/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            AssignmentActivity assignmentactivity = db.AssignmentActivities.Find(id);
            db.AssignmentActivities.Remove(assignmentactivity);
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