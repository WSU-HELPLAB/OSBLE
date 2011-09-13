using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Controllers
{
    [Authorize]
    public class MailController : OSBLEController
    {
        //
        // GET: /Mail/

        public ViewResult Index()
        {
            ViewBag.BoxHeader = "Inbox";
            var mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderByDescending(m => m.Posted);
            return View(mails.ToList());
        }

        public ViewResult Outbox()
        {
            ViewBag.BoxHeader = "Outbox";
            var mails = db.Mails.Where(m => m.FromUserProfileID == CurrentUser.ID).OrderByDescending(m => m.Posted);
            return View("Index", mails.ToList());
        }

        //
        // GET: /Mail/Details/5

        public ActionResult View(int id)
        {
            Mail mail = db.Mails.Find(id);

            // Mail not found
            if (mail == null)
            {
                return View("NotFound");
            }
            // Unauthorized mail to view.
            if ((mail.ToUserProfile != currentUser) && (mail.FromUserProfile != currentUser))
            {
                return RedirectToAction("Index");
            }
            else if ((mail.ToUserProfile == currentUser) && (mail.Read == false))
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
                    int courseID = cu.AbstractCourseID;
                    CoursesUsers ourCu = currentCourses.Where(c => c.AbstractCourseID == courseID).FirstOrDefault();
                    CoursesUsers theirCu = db.CoursesUsers.Where(c => (c.AbstractCourseID == courseID) && (c.UserProfileID == u.ID)).FirstOrDefault();

                    if ((ourCu != null) && (theirCu != null) && (!(ourCu.AbstractRole.Anonymized) || (theirCu.AbstractRole.CanGrade == true)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public ActionResult Create()
        {
            ViewBag.MailHeader = "New Message";

            int id = Convert.ToInt32(Request.Params["recipientID"]);
            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();

            // Handles Reply and Forward
            if (Request.Params["replyTo"] != null)
            {
                var replyto = Convert.ToInt32(Request.Params["replyTo"]);
                Mail reply = db.Mails.Find(replyto);
                // Ensure valid reply user
                if ((reply != null) && (reply.ToUserProfile == currentUser))
                {
                    string suffix = "RE: ";
                    if (id == 0 ) // forward 0 is passed for the recipientid on forwards
                    {
                        suffix = "FW: ";
                    }
                    ViewBag.MailHeader = mail.Subject = suffix + reply.Subject;
                    // Prefix each line with a '> '
                    mail.Message = "\n\nOriginal Message \nFrom: " + reply.FromUserProfile.FirstName + " " +
                                    reply.FromUserProfile.LastName + "\nSent at: " + reply.Posted.ToString() + "\n\n" +
                                    Regex.Replace(reply.Message, "^.*$", "> $&",
                                    RegexOptions.Multiline);

                }
            }
            if (id > 0 && canMail(id))
            {
                //adding recipients
                UserProfile u = db.UserProfiles.Find(id);
                recipientList.Add(u);

                Session["mail_recipients"] = recipientList;
            }
            return View(mail);
        }

        //
        // POST: /Mail/Create
        [HttpPost]
        public ActionResult Create(Mail mail)
        {
            if (ModelState.IsValid)
            {
                string recipient_string = Request.Params["recipientlist"];
                string[] recipients;
                List<int> reply_ids = new List<int>();

                // If it is a reply
                if (Session["mail_recipients"] != null)
                {
                    List<UserProfile> recipientList = new List<UserProfile>();
                    recipientList = Session["mail_recipients"] as List<UserProfile>;
                    Session["mail_recipients"] = null;
                    foreach (UserProfile up in recipientList)
                    {
                        if (recipient_string != "")
                        {
                            recipient_string += "," + up.ID.ToString();
                        }
                        else
                        {
                            recipient_string = up.ID.ToString();
                        }
                    }
                }
                if (recipient_string != null)
                {
                    recipients = recipient_string.Split(',');

                    foreach (string id in recipients)
                    {
                        Mail newMail = new Mail();
                        newMail.FromUserProfileID = CurrentUser.ID;
                        newMail.Read = false;
                        newMail.ToUserProfileID = Convert.ToInt32(id);
                        newMail.Subject = mail.Subject;
                        newMail.Message = mail.Message;
                        newMail.ThreadID = mail.ID;

                        using (NotificationController nc = new NotificationController())
                        {
                            nc.SendMailNotification(newMail);
                        }

                        db.Mails.Add(newMail);
                        db.SaveChanges();
                    }

                    return RedirectToAction("Index");
                }
            }
            return View(mail);
        }

        //
        // GET: /Mail/Delete/5
        [HttpPost]
        public ActionResult Delete(int id)
        {
            Mail mail = db.Mails.Find(id);

            if (mail.ToUserProfile == currentUser)
            {
                db.Mails.Remove(mail);
                db.SaveChanges();
            }
            else
            {
                Response.StatusCode = 403;
                return View("_AjaxEmpty");
            }

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
                .Where(c => c.AbstractRole.Anonymized == false)
                .Select(c => c.AbstractCourseID)
                .ToList();

            List<UserProfile> authorizedUsers = db.CoursesUsers
                .Where(c => authorizedCourses.Contains(c.AbstractCourseID))
                .Select(c => c.UserProfile)
                .ToList();

            authorizedUsers.Add(currentUser); // Add ourselves just in case we're not in a course.

            // If we are anonymous, limit search to ourselves plus instructors/TAs
            List<int> addedCourses = currentCourses
                .Where(c => c.AbstractRole.Anonymized == true)
                .Select(c => c.AbstractCourseID)
                .ToList();

            List<UserProfile> addedUsers = db.CoursesUsers
                .Where(c => addedCourses.Contains(c.AbstractCourseID) && ((c.UserProfileID == currentUser.ID) || (c.AbstractRole.CanGrade == true)))
                .Select(c => c.UserProfile)
                .ToList();

            // Combine lists into one distinct list of users, removing all pending users.
            List<UserProfile> users = authorizedUsers.Union(addedUsers).Where(u => u.UserName != null).OrderBy(u => u.LastName).Distinct().ToList();

            // Search list for our search string
            users = users.Where(u => (u.FirstName + " " + u.LastName).ToLower().IndexOf(term) != -1).ToList();

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