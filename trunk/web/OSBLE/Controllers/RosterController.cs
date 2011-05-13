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

    

    public class RosterController : OSBLEController
    {
        

        public class UsersByRole
        {
            public string RoleName
            {
                get;
                set;
            }

            public List<UserProfile> Users
            {
                get;
                set;
            }

            public int Count
            {
                get;
                set;
            }
        }
        //
        // GET: /Roster/

        public ViewResult Index()
        {
            var users = (from c in db.CoursesUsers
                         where c.CourseID == activeCourse.CourseID
                         select c);

            List<UsersByRole> usersbyRoles = new List<UsersByRole>();

            //Since we are using the db above we need a new one for the below operation
            OSBLEContext db2 = new OSBLEContext();

            foreach (CourseRole role in db2.CourseRoles)
            {
                UsersByRole usersByRole = new UsersByRole();
                usersByRole.RoleName = role.Name;
                usersByRole.Users = new List<UserProfile>(from c in users 
                                                          where role.ID == c.CourseRole.ID
                                                          select c.UserProfile);
                usersByRole.Count = usersByRole.Users.Count;

                usersbyRoles.Add(usersByRole);
            }

            //reverse it so the least important people are first
            usersbyRoles.Reverse();

            ViewBag.UsersByRoles = usersbyRoles;

            return View();
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
                coursesusers.CourseID = activeCourse.Course.ID;
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

        public ActionResult Edit(int userProfileID)
        {
            CoursesUsers coursesusers = getCoursesUsers(userProfileID);
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

        public ActionResult Delete(int userProfileID)
        {
            CoursesUsers coursesusers = getCoursesUsers(userProfileID);
            return View(coursesusers);
        }

        //
        // POST: /Roster/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int userProfileID)
        {
            CoursesUsers coursesusers = getCoursesUsers(userProfileID);
            db.CoursesUsers.Remove(coursesusers);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private CoursesUsers getCoursesUsers(int userProfileId)
        {
            return (from c in db.CoursesUsers
                    where c.CourseID == activeCourse.CourseID
                    && c.UserProfileID == userProfileId
                    select c).FirstOrDefault();
        }

    }
}