using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.HomePage;
using OSBLE.Attributes;
using OSBLE.Models.Users;
using System.Web.Security;

namespace OSBLE.Controllers
{
    [Authorize]
    [IsAdmin]
    public class AdminController : OSBLEController
    {
        public AdminController()
            : base()
        {
            ViewBag.CurrentTab = "Administration";
        }
        //
        // GET: /Admin/

        public ViewResult Index()
        {
            // Get list of users (other than current user) who are not pending, ordered by last name
            var userprofiles = db.UserProfiles.Where(u => u.ID != currentUser.ID && u.UserName != null).OrderBy(u => u.LastName).Include(u=>u.School);
            // Add pending users to the end of the list
            userprofiles = userprofiles.Union(db.UserProfiles.Where(u => u.ID != currentUser.ID && u.UserName == null).Include(u => u.School));
            return View(userprofiles.ToList());
        }

        //
        // GET: /Admin/Details/5

        public ViewResult Details(int id)
        {
            UserProfile userprofile = db.UserProfiles.Find(id);
            return View(userprofile);
        }

        public ActionResult Impersonate(int id)
        {
            UserProfile u = db.UserProfiles.Find(id);

            FormsAuthentication.SignOut();
            context.Session.Clear();
            FormsAuthentication.SetAuthCookie(u.UserName, false);

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Admin/Edit/5

        public ActionResult Edit(int id)
        {
            UserProfile userprofile = db.UserProfiles.Find(id);
            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name", userprofile.SchoolID);
            return View(userprofile);
        }

        //
        // POST: /Admin/Edit/5

        [HttpPost]
        public ActionResult Edit(UserProfile userprofile)
        {
            if (ModelState.IsValid)
            {
                db.Entry(userprofile).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name", userprofile.SchoolID);
            return View(userprofile);
        }

        //
        // GET: /Admin/Delete/5

        public ActionResult Delete(int id)
        {
            UserProfile userprofile = db.UserProfiles.Find(id);
            return View(userprofile);
        }

        //
        // POST: /Admin/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            
            
            UserProfile userprofile = db.UserProfiles.Find(id);
            
            if(userprofile != null) {
                Membership.DeleteUser(userprofile.UserName);
                db.UserProfiles.Remove(userprofile);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

    }
}
