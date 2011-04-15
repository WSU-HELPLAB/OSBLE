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
    public class RosterController : Controller
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /Roster/

        public ViewResult Index()
        {
            var coursesusers = db.CoursesUsers.Include(c => c.UserProfile).Include(c => c.Course).Include(c => c.CourseRole);
            return View(coursesusers.ToList());
        }

        //
        // GET: /Roster/Details/5

        public ViewResult Details(int id)
        {
            CoursesUsers coursesusers = db.CoursesUsers.Find(id);
            return View(coursesusers);
        }

        //
        // GET: /Roster/Create

        public ActionResult Create()
        {
            ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName");
            ViewBag.CourseID = new SelectList(db.Courses, "ID", "Prefix");
            ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name");
            return View();
        } 

        //
        // POST: /Roster/Create

        [HttpPost]
        public ActionResult Create(CoursesUsers coursesusers)
        {
            if (ModelState.IsValid)
            {
                db.CoursesUsers.Add(coursesusers);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", coursesusers.UserProfileID);
            ViewBag.CourseID = new SelectList(db.Courses, "ID", "Prefix", coursesusers.CourseID);
            ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name", coursesusers.CourseRoleID);
            return View(coursesusers);
        }
        
        //
        // GET: /Roster/Edit/5
 
        public ActionResult Edit(int id)
        {
            CoursesUsers coursesusers = db.CoursesUsers.Find(id);
            ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", coursesusers.UserProfileID);
            ViewBag.CourseID = new SelectList(db.Courses, "ID", "Prefix", coursesusers.CourseID);
            ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name", coursesusers.CourseRoleID);
            return View(coursesusers);
        }

        //
        // POST: /Roster/Edit/5

        [HttpPost]
        public ActionResult Edit(CoursesUsers coursesusers)
        {
            if (ModelState.IsValid)
            {
                db.Entry(coursesusers).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", coursesusers.UserProfileID);
            ViewBag.CourseID = new SelectList(db.Courses, "ID", "Prefix", coursesusers.CourseID);
            ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name", coursesusers.CourseRoleID);
            return View(coursesusers);
        }

        //
        // GET: /Roster/Delete/5
 
        public ActionResult Delete(int id)
        {
            CoursesUsers coursesusers = db.CoursesUsers.Find(id);
            return View(coursesusers);
        }

        //
        // POST: /Roster/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            CoursesUsers coursesusers = db.CoursesUsers.Find(id);
            db.CoursesUsers.Remove(coursesusers);
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