using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;
using System;
using System.Web;
using System.Collections.Generic;

namespace OSBLE.Controllers
{
    [Authorize]
    public class MailController : OSBLEController
    {
        //
        // GET: /Mail/

        public ViewResult Index()
        {
            var mails = db.Mails.Where(m=>m.ToUserProfileID == CurrentUser.ID).OrderByDescending(m=>m.Posted);
            return View(mails.ToList());
        }

        //
        // GET: /Mail/Details/5

        public ViewResult View(int id)
        {
            Mail mail = db.Mails.Find(id);
            if (mail.Read == false)
            {
                mail.Read = true;
                db.SaveChanges();
                SetUnreadMessageCount();
            }
            return View(mail);
        }

        //
        // GET: /Mail/Create

        /// <summary>
        /// Checks if the current users course privileges allows them to mail the requested user.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool canMail(int id)
        {
            UserProfile u = db.UserProfiles.Find(id);

            if (id == currentUser.ID)
            {
                return true;
            }
            else
            {
                foreach (CoursesUsers cu in currentCourses)
                {
                    int courseID = cu.CourseID;
                    CoursesUsers ourCu = currentCourses.Where(c => c.CourseID == courseID).FirstOrDefault();
                    CoursesUsers theirCu = db.CoursesUsers.Where(c => (c.CourseID == courseID) && (c.UserProfileID == u.ID)).FirstOrDefault();

                    if ((ourCu != null) && (theirCu != null) && (!(ourCu.CourseRole.Anonymized) || (theirCu.CourseRole.CanGrade == true)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public ActionResult Create()
        {
            int id = Convert.ToInt32(Request.Params["recipientID"]);
            if (!canMail(id))
            {
                return RedirectToAction("Index");
            }

            Mail mail = new Mail();

            UserProfile u = db.UserProfiles.Find(id);
            mail.ToUserProfile = u;
            mail.ToUserProfileID = u.ID;

            return View(mail);
        }

        //
        // POST: /Mail/Create

        [HttpPost]
        public ActionResult Create(Mail mail)
        {
            if (!canMail(mail.ToUserProfileID))
            {
                return RedirectToAction("Index");
            }

                if (ModelState.IsValid)
                {
                    mail.FromUserProfileID = CurrentUser.ID;
                    mail.FromUserProfile = CurrentUser;
                    mail.Read = false;

                    db.Mails.Add(mail);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

            return View(mail);
        }

        //
        // GET: /Mail/Delete/5

        public ActionResult Delete(int id)
        {
            Mail mail = db.Mails.Find(id);
            return View(mail);
        }

        //
        // POST: /Mail/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Mail mail = db.Mails.Find(id);
            db.Mails.Remove(mail);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private class AutocompleteObject
        {
            public int value { get; set; }
            public string label { get; set; }

            public AutocompleteObject(int value, string label)
            {
                this.value = value;
                this.label = label;
            }
        }

        /// <summary>
        /// Autocomplete Search for Mail. Returns JSON.
        /// </summary>
        /// <returns></returns>
        public ActionResult Search()
        {
            string term = Request.Params["term"];

            // If we are not anonymous in a course, allow search of all users.
            List<int> authorizedCourses = currentCourses
                .Where(c=>c.CourseRole.Anonymized==false)
                .Select(c=>c.CourseID)
                .ToList();

            List<UserProfile> authorizedUsers = db.CoursesUsers
                .Where(c => authorizedCourses.Contains(c.CourseID))
                .Select(c => c.UserProfile)
                .ToList();

            // If we are anonymous, limit search to ourselves plus instructors/TAs
            List<int> addedCourses = currentCourses
                .Where(c => c.CourseRole.Anonymized==true)
                .Select(c => c.CourseID)
                .ToList();

            List<UserProfile> addedUsers = db.CoursesUsers
                .Where(c => addedCourses.Contains(c.CourseID) && ((c.UserProfileID == currentUser.ID) || (c.CourseRole.CanGrade == true)))
                .Select(c => c.UserProfile)
                .ToList();

            // Combine lists into one distinct list of users, removing all pending users.
            List<UserProfile> users = authorizedUsers.Union(addedUsers).Where(u=>u.UserName != null).OrderBy(u=>u.LastName).Distinct().ToList();

            // Search list for our search string
            users = users.Where(u=>(u.FirstName + " " + u.LastName).ToLower().IndexOf(term) != -1).ToList();

            List<AutocompleteObject> outputList = new List<AutocompleteObject>();

            foreach (UserProfile u in users)
            {
                outputList.Add(new AutocompleteObject(u.ID, u.FirstName + " " + u.LastName));
            }

            return Json(outputList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, FileCache(Duration = 3600)]
        public FileStreamResult ProfilePicture(int id)
        {
            bool show = canMail(id);

            if (show == true)
            {
                UserProfile u = db.UserProfiles.Find(id);
                return new FileStreamResult(FileSystem.GetProfilePictureOrDefault(u), "image/jpeg");
            }
            else
            {
                return new FileStreamResult(FileSystem.GetDefaultProfilePicture(), "image/jpeg");
            }
        }

    }
}
