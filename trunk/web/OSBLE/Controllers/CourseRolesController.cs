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
    public class CourseRolesController : Controller
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /CourseRoles/

        public ViewResult Index()
        {
            return View(db.CourseRoles.ToList());
        }

        //
        // GET: /CourseRoles/Details/5

        public ViewResult Details(int id)
        {
            CourseRole courserole = db.CourseRoles.Find(id);
            return View(courserole);
        }

        //
        // GET: /CourseRoles/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /CourseRoles/Create

        [HttpPost]
        public ActionResult Create(CourseRole courserole)
        {
            if (ModelState.IsValid)
            {
                db.CourseRoles.Add(courserole);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            return View(courserole);
        }
        
        //
        // GET: /CourseRoles/Edit/5
 
        public ActionResult Edit(int id)
        {
            CourseRole courserole = db.CourseRoles.Find(id);
            return View(courserole);
        }

        //
        // POST: /CourseRoles/Edit/5

        [HttpPost]
        public ActionResult Edit(CourseRole courserole)
        {
            if (ModelState.IsValid)
            {
                db.Entry(courserole).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(courserole);
        }

        //
        // GET: /CourseRoles/Delete/5
 
        public ActionResult Delete(int id)
        {
            CourseRole courserole = db.CourseRoles.Find(id);
            return View(courserole);
        }

        //
        // POST: /CourseRoles/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            CourseRole courserole = db.CourseRoles.Find(id);
            db.CourseRoles.Remove(courserole);
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