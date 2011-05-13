using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    [Authorize]
    public class SchoolController : OSBLEController
    {
        //
        // GET: /School/

        public ViewResult Index()
        {
            return View(db.Schools.ToList());
        }

        //
        // GET: /School/Details/5

        public ViewResult Details(int id)
        {
            School school = db.Schools.Find(id);
            return View(school);
        }

        //
        // GET: /School/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /School/Create

        [HttpPost]
        public ActionResult Create(School school)
        {
            if (ModelState.IsValid)
            {
                db.Schools.Add(school);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(school);
        }

        //
        // GET: /School/Edit/5

        public ActionResult Edit(int id)
        {
            School school = db.Schools.Find(id);
            return View(school);
        }

        //
        // POST: /School/Edit/5

        [HttpPost]
        public ActionResult Edit(School school)
        {
            if (ModelState.IsValid)
            {
                db.Entry(school).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(school);
        }

        //
        // GET: /School/Delete/5

        public ActionResult Delete(int id)
        {
            School school = db.Schools.Find(id);
            return View(school);
        }

        //
        // POST: /School/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            School school = db.Schools.Find(id);
            db.Schools.Remove(school);
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