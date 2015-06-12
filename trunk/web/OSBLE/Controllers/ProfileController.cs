using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using OSBLE.Models;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{    
    [Authorize]
    public class ProfileController : OSBLEController
    {
        //
        // GET: /Profile/

        public ViewResult Index()
        {
            var userprofiles = db.UserProfiles.Include(u => u.School);
            return View(userprofiles.ToList());
        }

        //
        // GET: /Profile/Details/5

        public ViewResult Details(int id)
        {
            UserProfile userprofile = db.UserProfiles.Find(id);
            return View(userprofile);
        }

        //
        // GET: /Profile/Create

        public ActionResult Create()
        {
            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name");
            return View();
        }

        //
        // POST: /Profile/Create

        [HttpPost]
        public ActionResult Create(UserProfile userprofile)
        {
            if (ModelState.IsValid)
            {
                db.UserProfiles.Add(userprofile);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name", userprofile.SchoolID);
            return View(userprofile);
        }

        //
        // GET: /Profile/Edit/5

        public ActionResult Edit(int id)
        {
            UserProfile userprofile = db.UserProfiles.Find(id);
            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name", userprofile.SchoolID);
            return View(userprofile);
        }

        //
        // POST: /Profile/Edit/5

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
        // GET: /Profile/Delete/5

        public ActionResult Delete(int id)
        {
            UserProfile userprofile = db.UserProfiles.Find(id);
            return View(userprofile);
        }

        //
        // POST: /Profile/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            UserProfile userprofile = db.UserProfiles.Find(id);
            Membership.DeleteUser(userprofile.UserName); // Also delete the user from the ASP.NET database.

            db.UserProfiles.Remove(userprofile);
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