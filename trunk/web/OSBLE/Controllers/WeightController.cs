using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models.Courses;

namespace OSBLE.Controllers
{
    public class WeightController : OSBLEController
    {
        //
        // GET: /Weight/

        public ViewResult Index()
        {
            return View(db.Weights.ToList());
        }

        //
        // GET: /Weight/Details/5

        public ViewResult Details(string id)
        {
            Weight weight = db.Weights.Find(id);
            return View(weight);
        }

        //
        // GET: /Weight/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Weight/Create

        [HttpPost]
        public ActionResult Create(Weight weight)
        {
            if (ModelState.IsValid)
            {
                db.Weights.Add(weight);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(weight);
        }

        //
        // GET: /Weight/Edit/5

        public ActionResult Edit(string id)
        {
            Weight weight = db.Weights.Find(id);
            return View(weight);
        }

        //
        // POST: /Weight/Edit/5

        [HttpPost]
        public ActionResult Edit(Weight weight)
        {
            if (ModelState.IsValid)
            {
                db.Entry(weight).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(weight);
        }

        //
        // GET: /Weight/Delete/5

        public ActionResult Delete(string id)
        {
            Weight weight = db.Weights.Find(id);
            return View(weight);
        }

        //
        // POST: /Weight/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            Weight weight = db.Weights.Find(id);
            db.Weights.Remove(weight);
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