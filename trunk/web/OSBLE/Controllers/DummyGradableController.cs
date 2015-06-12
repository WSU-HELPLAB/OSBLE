using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;

namespace OSBLE.Controllers
{ 
    public class DummyGradableController : Controller
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /DummyGradable/

        public ViewResult Index()
        {
            var abstractgradables = db.AbstractGradables.Include(g => g.Weight);
            return View(abstractgradables.ToList());
        }

        //
        // GET: /DummyGradable/Details/5

        public ViewResult Details(int id)
        {
            Gradable gradable = db.Gradables.Find(id);
            return View(gradable);
        }

        //
        // GET: /DummyGradable/Create

        public ActionResult Create()
        {
            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name");
            return View();
        } 

        //
        // POST: /DummyGradable/Create

        [HttpPost]
        public ActionResult Create(Gradable gradable)
        {
            if (ModelState.IsValid)
            {
                db.AbstractGradables.Add(gradable);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name", gradable.WeightID);
            return View(gradable);
        }
        
        //
        // GET: /DummyGradable/Edit/5
 
        public ActionResult Edit(int id)
        {
            Gradable gradable = db.Gradables.Find(id);
            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name", gradable.WeightID);
            return View(gradable);
        }

        //
        // POST: /DummyGradable/Edit/5

        [HttpPost]
        public ActionResult Edit(Gradable gradable)
        {
            if (ModelState.IsValid)
            {
                db.Entry(gradable).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.WeightID = new SelectList(db.Weights, "ID", "Name", gradable.WeightID);
            return View(gradable);
        }

        //
        // GET: /DummyGradable/Delete/5
 
        public ActionResult Delete(int id)
        {
            Gradable gradable = db.Gradables.Find(id);
            return View(gradable);
        }

        //
        // POST: /DummyGradable/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Gradable gradable = db.Gradables.Find(id);
            db.AbstractGradables.Remove(gradable);
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