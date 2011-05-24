using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{
    [NotForCommunity]
    public class AssignmentController : OSBLEController
    {
        //
        // GET: /Assignment/
        public AssignmentController() : base()
        {
            ViewBag.CurrentTab = "Assignments";
        }

        public ViewResult Index()
        {
            return View(db.Assignments.ToList());
        }

        //
        // GET: /Assignment/Details/5

        public ViewResult Details(string id)
        {
            Assignment assignment = db.Assignments.Find(id);
            return View(assignment);
        }

        //
        // GET: /Assignment/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Assignment/Create

        [HttpPost]
        public ActionResult Create(Assignment assignment)
        {
            if (ModelState.IsValid)
            {
                db.Assignments.Add(assignment);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(assignment);
        }

        //
        // GET: /Assignment/Edit/5

        public ActionResult Edit(string id)
        {
            Assignment assignment = db.Assignments.Find(id);
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
            return View(assignment);
        }

        //
        // GET: /Assignment/Delete/5

        public ActionResult Delete(string id)
        {
            Assignment assignment = db.Assignments.Find(id);
            return View(assignment);
        }

        //
        // POST: /Assignment/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            Assignment assignment = db.Assignments.Find(id);
            db.Assignments.Remove(assignment);
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