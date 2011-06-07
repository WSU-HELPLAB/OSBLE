using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Rubrics;
using OSBLE.Models;

namespace OSBLE.Controllers
{ 
    public class RubricController : Controller
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /Rubric/

        public ViewResult Index()
        {
            return View(db.Rubrics.ToList());
        }

        //
        // GET: /Rubric/Details/5

        public ViewResult Details(int id)
        {
            Rubric rubric = db.Rubrics.Find(id);
            return View(rubric);
        }

        //
        // GET: /Rubric/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /Rubric/Create

        [HttpPost]
        public ActionResult Create(Rubric rubric)
        {
            if (ModelState.IsValid)
            {
                db.Rubrics.Add(rubric);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            return View(rubric);
        }
        
        //
        // GET: /Rubric/Edit/5
 
        public ActionResult Edit(int id)
        {
            Rubric rubric = db.Rubrics.Find(id);
            return View(rubric);
        }

        //
        // POST: /Rubric/Edit/5

        [HttpPost]
        public ActionResult Edit(Rubric rubric)
        {
            if (ModelState.IsValid)
            {
                db.Entry(rubric).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(rubric);
        }

        //
        // GET: /Rubric/Delete/5
 
        public ActionResult Delete(int id)
        {
            Rubric rubric = db.Rubrics.Find(id);
            return View(rubric);
        }

        //
        // POST: /Rubric/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            Rubric rubric = db.Rubrics.Find(id);
            db.Rubrics.Remove(rubric);
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