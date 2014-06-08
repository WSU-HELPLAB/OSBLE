using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using OSBLE.Attributes;
using OSBLE.Models.Users;
using OSBLE.Utility;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    [IsAdmin]
#if !DEBUG
    //[RequireHttps]
#endif
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
            // We'll check for the existence of the departments list file and 
            // show or hide the ABET outcomes link based on this
            OSBLE.Models.FileSystem.OSBLEDirectory fs =
                Models.FileSystem.Directories.GetAdmin();
            bool showLink = false;
            if (fs.File("departments.txt").Count > 0)
            {
                showLink = true;
            }
            ViewBag.ShowABETOutcomesLink = showLink;
            
            // Get list of users (other than current user) ordered by last name, who are not pending
            List<UserProfile> userprofiles = db.UserProfiles.Where(u => u.ID != CurrentUser.ID && u.UserName != null).OrderBy(u => u.LastName).Include(u => u.School).ToList();
            // Add pending users to bottom of list.
            userprofiles = userprofiles.Concat(db.UserProfiles.Where(u => u.ID != CurrentUser.ID && u.UserName == null).ToList()).ToList();
            return View(userprofiles);
        }

        //
        // GET: /Admin/Details/5

        public ActionResult Details(int id = -1)
        {
            if (id != -1)
            {
                UserProfile userprofile = db.UserProfiles.Find(id);
                return View(userprofile);
            }

            return RedirectToAction("Index", "Admin");
        }

        public ActionResult Impersonate(int id)
        {
            UserProfile u = db.UserProfiles.Find(id);

            OsbleAuthentication.LogIn(u);
            context.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Admin/Edit/5

        public ActionResult Edit(int id = -1)
        {
            if (id != -1)
            {
                UserProfile userprofile = db.UserProfiles.Find(id);
                ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name", userprofile.SchoolID);
                return View(userprofile);
            }

            return RedirectToAction("Index", "Admin");
        }

        //
        // POST: /Admin/Edit/5

        [HttpPost]
        public ActionResult Edit(UserProfile userprofile)
        {
            //make sure that the user name isn't already taken
            int count = (from user in db.UserProfiles
                         where user.UserName.ToLower().CompareTo(userprofile.UserName.ToLower()) == 0
                         && user.ID != userprofile.ID
                         select user).Count();

            if (count != 0)
            {
                ModelState.AddModelError("UserName", "User name already taken.");
            }
            
            if (ModelState.IsValid)
            {
                UserProfile dbUser = db.UserProfiles.Where(u => u.ID == userprofile.ID).FirstOrDefault();
                db.Entry(dbUser).State = EntityState.Detached;

                if (userprofile.Password == null || userprofile.Password.Length == 0)
                {
                    userprofile.Password = dbUser.Password;
                }
                else
                {
                    userprofile.Password = UserProfile.GetPasswordHash(userprofile.Password);
                }

                db.Entry(userprofile).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.SchoolID = new SelectList(db.Schools, "ID", "Name", userprofile.SchoolID);
            return View(userprofile);
        }

        //
        // GET: /Admin/Delete/5

        public ActionResult Delete(int id = -1)
        {
            if (id != -1)
            {
                UserProfile userprofile = db.UserProfiles.Find(id);
                return View(userprofile);
            }

            return RedirectToAction("Index", "Admin");
        }

        //
        // POST: /Admin/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            UserProfile userprofile = db.UserProfiles.Find(id);

            userprofile.CanCreateCourses = false;
            userprofile.IsAdmin = false;
            userprofile.Identification = "";
            userprofile.FirstName = "Deleted";
            userprofile.LastName = "User";
            userprofile.SchoolID = 1;
            userprofile.EmailAllNotifications = false;
            userprofile.UserName = "DeleteUser";

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