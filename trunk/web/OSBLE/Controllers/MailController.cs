﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.HomePage;
using OSBLE.Models.Assignments;

namespace OSBLE.Controllers
{
    [Authorize]
    public class MailController : OSBLEController
    {
        //
        // GET: /Mail/
        public ViewResult Index(int? sortBy)
        {
            ViewBag.BoxHeader = "Inbox";
            List<Mail> mails = new List<Mail>();
            bool reverse = false;
            //setting up the sort order for email default is by posted date
            UserProfile cu = db.UserProfiles.Where(u => u.ID == CurrentUser.ID).FirstOrDefault();
            if (sortBy != null)
            {
                if (cu.SortBy == sortBy)
                {
                    reverse = true;
                }
                cu.SortBy = (int)sortBy;
                db.SaveChanges();
            }
            else
            {
                cu.SortBy = (int)UserProfile.sortEmailBy.POSTED;
                db.SaveChanges();
            }

            switch ((UserProfile.sortEmailBy)CurrentUser.SortBy)
            {
                case UserProfile.sortEmailBy.POSTED:
                    if (!reverse)
                    {
                        mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderByDescending(m => m.Posted).ToList();
                    }
                    else // reverse
                    {
                        mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderBy(m => m.Posted).ToList();
                    }
                    break;
                case UserProfile.sortEmailBy.CONTEXT:
                    if (!reverse)
                    {
                        mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderBy(m => m.Context.Name).ToList();
                    }
                    else // reverse
                    {
                        mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderByDescending(m => m.Context.Name).ToList();
                    }
                    break;
                case UserProfile.sortEmailBy.FROM:
                    if (!reverse)
                    {
                        mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderBy(m => m.FromUserProfile.FirstName).ToList();
                    }
                    else // reverse
                    {
                        mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderByDescending(m => m.FromUserProfile.FirstName).ToList();
                    }
                    break;
                case UserProfile.sortEmailBy.SUBJECT:
                    if (!reverse)
                    {
                        mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderBy(m => m.Subject).ToList();
                    }
                    else // reverse
                    {
                        mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID).OrderByDescending(m => m.Subject).ToList();
                    }
                    break;
                default:
                    break;
            }
            // hackish way to handle the second reverse  couldn't think of a better way to do it. (KD) might look into it later.
            if (reverse)
            {
                cu.SortBy = -1;
                db.SaveChanges();
            }
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
                
                // Removes the notification once the email is read.
                Notification n = (from notification in db.Notifications
                                  where notification.ItemID == id
                                  select notification).FirstOrDefault();

                // Notification exists and belongs to current user.
                if ((n != null) && (n.RecipientID == currentUser.ID))
                {
                    // Mark notification as read.
                    n.Read = true;
                    db.SaveChanges();
                }

                db.SaveChanges();
                SetUnreadMessageCount();
            }

            // getting all recipients of the mail
            List<UserProfile> recipients = (from m in db.Mails
                                            where m.ThreadID == mail.ThreadID 
                                            select m.ToUserProfile).ToList();

            // for the older message and newer message links
            int i = 1, j = 1;
            var lastMail = (from m in db.Mails
                            where m.ToUserProfileID == currentUser.ID
                            orderby m.ID descending
                            select m).FirstOrDefault();

            var next = id;
            // == null handles deleted emails
            // toUserProfile handes the issue of switching to the outbox
            while ((db.Mails.Find(id + i) == null || db.Mails.Find(id + i).ToUserProfileID != currentUser.ID) && ((id + i) <= lastMail.ID))
            {
                i++;
            }
            if (id + i <= lastMail.ID)
            {
                next = id + i;
            }
            ViewBag.Next = next;

            var prev = id;
            // == null handles deleted emails
            // toUserProfile handes the issue of switching to the outbox
            while ((db.Mails.Find(id - j) == null || db.Mails.Find(id - j).ToUserProfileID != currentUser.ID) && j < id)
            {
                j++;
            }
            if (j < id)
            {
                prev = id - j;
            }
            ViewBag.Prev = prev;

            ViewBag.AllRecipients = recipients;
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
                foreach (CourseUser cu in currentCourses)
                {
                    int courseID = cu.AbstractCourseID;
                    CourseUser ourCu = currentCourses.Where(c => c.AbstractCourseID == courseID).FirstOrDefault();
                    CourseUser theirCu = db.CourseUsers.Where(c => (c.AbstractCourseID == courseID) && (c.UserProfileID == u.ID)).FirstOrDefault();

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
            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();

            // gets the current courseid
            mail.ContextID = (int)context.Session["ActiveCourse"];

            Session["mail_recipients"] = recipientList;
            return View("Create", mail);
        }

        public ActionResult CreateInstructor()
        {
            ViewBag.MailHeader = "New Instructor Message";

            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();

            List<CourseUser> instructors = db.CourseUsers.Where(c => (c.AbstractRole.Name == "Instructor" && c.AbstractCourseID == activeCourse.AbstractCourseID)).ToList();
            if (instructors != null)
            {
                foreach (CourseUser cu in instructors)
                {
                    recipientList.Add(cu.UserProfile);
                }
            }
            Session["mail_recipients"] = recipientList;
            return View("Create", mail);
        }

        public ActionResult CreateTA()
        {
            ViewBag.MailHeader = "New TA(s) Message";

            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();

            List<CourseUser> tas = db.CourseUsers.Where(c => (c.AbstractRole.Name == "TA" && c.AbstractCourseID == activeCourse.AbstractCourseID)).ToList();
            if (tas != null)
            {
                foreach (CourseUser cu in tas)
                {
                    recipientList.Add(cu.UserProfile);
                }
            }

            Session["mail_recipients"] = recipientList;
            return View("Create", mail);
        }

        public ActionResult CreateInstructorTA()
        {
            ViewBag.MailHeader = "New Instructor and TA(s) Message";

            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();

            List<CourseUser> instructorTA = db.CourseUsers.Where(c => ((c.AbstractRole.Name == "Instructor" || c.AbstractRole.Name == "TA") && c.AbstractCourseID == activeCourse.AbstractCourseID)).ToList();
            if (instructorTA != null)
            {
                foreach (CourseUser cu in instructorTA)
                {
                    recipientList.Add(cu.UserProfile);
                }
            }

            Session["mail_recipients"] = recipientList;
            return View("Create", mail);
        }

        public ActionResult CreateUser(int id)
        {
            ViewBag.MailHeader = "New Message";

            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();

            CourseUser studentRec = db.CourseUsers.Where(c => (c.UserProfileID == id && c.AbstractCourseID == activeCourse.AbstractCourseID)).FirstOrDefault();
            if (studentRec != null)
            {
                recipientList.Add(studentRec.UserProfile);
            }

            Session["mail_recipients"] = recipientList;
            return View("Create", mail);
        }

        public ActionResult CreateTeamMember(int id)
        {
            ViewBag.MailHeader = "New Team Member Message";

            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();

            CourseUser studentRec = db.CourseUsers.Where(c => (c.UserProfileID == id && c.AbstractCourseID == activeCourse.AbstractCourseID)).FirstOrDefault();
            if (studentRec != null)
            {
                recipientList.Add(studentRec.UserProfile);
            }

            Session["mail_recipients"] = recipientList;
            return View("Create", mail);
        }

        public ActionResult CreateEntireTeam(int teamID)
        {
            ViewBag.MailHeader = "New Team Message";

            Mail mail = new Mail();
            mail.ContextID = activeCourse.ID;
            List<UserProfile> recipientList = new List<UserProfile>();

            Team team = db.Teams.Find(teamID);
            foreach (TeamMember tm in team.TeamMembers)
            {
                recipientList.Add(tm.CourseUser.UserProfile);
            }

            Session["mail_recipients"] = recipientList;
            return View("Create", mail);
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

                // gets the current course
                mail.Context = db.Courses.Where(b => b.ID == mail.ContextID).FirstOrDefault();

                if (recipient_string != null)
                {
                    recipients = recipient_string.Split(',');
                    int count = 0;
                    int threadID = 0;

                    foreach (string id in recipients)
                    {
                        Mail newMail = new Mail();
                        newMail.FromUserProfileID = CurrentUser.ID;
                        newMail.Read = false;
                        newMail.ToUserProfileID = Convert.ToInt32(id);
                        newMail.Subject = mail.Subject;
                        newMail.Message = mail.Message;
                        newMail.ThreadID = threadID;
                        newMail.ContextID = mail.ContextID;

                        //need to create the mail before we can send the notification and set the threadID
                        db.Mails.Add(newMail);
                        db.SaveChanges();

                        // need to have an email created to get a valid id to set the thread ids to.
                        if (count == 0)
                        {
                            threadID = newMail.ID;
                            newMail.ThreadID = newMail.ID;

                            db.SaveChanges();
                        }

                        using (NotificationController nc = new NotificationController())
                        {
                            nc.SendMailNotification(newMail);
                        }
                        ++count;
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
            string term = Request.Params["term"].ToString().ToLower();
            
            // If we are not anonymous in a course, allow search of all users.
            List<int> authorizedCourses = currentCourses
                .Where(c => c.AbstractRole.Anonymized == false)
                .Select(c => c.AbstractCourseID)
                .ToList();

            List<UserProfile> authorizedUsers = db.CourseUsers
                .Where(c => authorizedCourses.Contains(c.AbstractCourseID))
                .Select(c => c.UserProfile)
                .ToList();

            authorizedUsers.Add(currentUser); // Add ourselves just in case we're not in a course.

            // If we are anonymous, limit search to ourselves plus instructors/TAs
            List<int> addedCourses = currentCourses
                .Where(c => c.AbstractRole.Anonymized == true)
                .Select(c => c.AbstractCourseID)
                .ToList();

            List<UserProfile> addedUsers = db.CourseUsers
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
