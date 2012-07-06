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
    [OsbleAuthorize]
    public class MailController : OSBLEController
    {
        public MailController()
        {
            ViewBag.Cache = Cache;
        }

        //
        // GET: /Mail/
        public ViewResult Index(int? sortBy)
        {
            ViewBag.BoxHeader = "Inbox";
            List<Mail> mails = new List<Mail>();
            bool reverse = false;
            //setting up the sort order for email default is by posted date
            UserProfile cu = db.UserProfiles.Where(u => u.ID == CurrentUser.ID).FirstOrDefault();
            return View(mails.ToList());
        }

        public ViewResult Outbox(int? sortBy)
        {
            //ViewBag.BoxHeader = "Outbox";
            //var mails = db.Mails.Where(m => m.FromUserProfileID == CurrentUser.ID && !m.DeleteFromOutbox).OrderByDescending(m => m.Posted);
            //return View("Index", mails.ToList());

            ViewBag.BoxHeader = "Outbox";
            List<Mail> mails = new List<Mail>();
            bool reverse = false;
            //setting up the sort order for email default is by posted date
            UserProfile cu = db.UserProfiles.Where(u => u.ID == CurrentUser.ID).FirstOrDefault();
            
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
            if ((mail.ToUserProfile.ID != CurrentUser.ID) && (mail.FromUserProfile.ID != CurrentUser.ID))
            {
                return RedirectToAction("Index");
            }
            else if ((mail.ToUserProfile.ID == CurrentUser.ID) && (mail.Read == false))
            {
                mail.Read = true;

                // Removes the notification once the email is read.
                Notification n = (from notification in db.Notifications
                                  where notification.ItemID == id
                                  select notification).FirstOrDefault();

                // Notification exists and belongs to current user.
                if ((n != null) && (n.RecipientID == CurrentUser.ID))
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

            var next = id;
            var prev = id;

            if (mail.ToUserProfileID == CurrentUser.ID)
            {
                var inboxMail = (from m in db.Mails
                                 where m.ToUserProfileID == CurrentUser.ID &&
                                 !m.DeleteFromInbox 
                                 orderby m.ID ascending
                                 select m).ToList();


                //MG&KD:Loops through the mail (that is sorted ascending). Continues to set and replace "prev" while the ID is lower than the current mails ID
                //Eventually if there is an ID that is higher than the current, it will be the set to "next" and break. Setting both prev and next if there are valid values.
                foreach (Mail m in inboxMail)
                {
                    if (m.ID < id)
                    {
                        prev = m.ID;
                    }
                    if (m.ID > id)
                    {
                        next = m.ID;
                        break;
                    }
                }
            }
            else if (mail.FromUserProfileID == CurrentUser.ID)
            {
                var outboxMail = (from m in db.Mails
                                 where m.FromUserProfileID == CurrentUser.ID &&
                                 !m.DeleteFromOutbox
                                 orderby m.ID ascending
                                 select m).ToList();


                //MG&KD:Loops through the mail (that is sorted ascending). Continues to set and replace "prev" while the ID is lower than the current mails ID
                //Eventually if there is an ID that is higher than the current, it will be the set to "next" and break. Setting both prev and next if there are valid values.
                foreach (Mail m in outboxMail)
                {
                    if (m.ID < id)
                    {
                        prev = m.ID;
                    }
                    if (m.ID > id)
                    {
                        next = m.ID;
                        break;
                    }
                }
            }
            else //Mail is not to or from them, should not be viewing it!
            {
                return RedirectToAction("Index");
            }

            ViewBag.Next = next;
            ViewBag.Prev = prev;

            ViewBag.AllRecipients = recipients;
            return View(mail);
        }

        /// <summary>
        /// Checks if the current users course privileges allows them to mail the requested user.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool canMail(int userProfileId)
        {
            if (userProfileId == CurrentUser.ID)
            {
                return true;
            }
            else
            {
                UserProfile u = db.UserProfiles.Find(userProfileId);
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

        /// <summary>
        /// This function sets up the ViewBags for the mail view.
        /// </summary>
        /// <param name="InitialRecipients">anyone to be initially added to the mail should be in this list, if there are no initial recipients this parameter is optional</param>
        public void setUpMailViewBags(List<UserProfile> InitialRecipients = null)
        {
            List<CourseUser> TaAndInstructorList = db.CourseUsers
                    .Where(cu => (cu.AbstractRoleID == (int)CourseRole.CourseRoles.TA || cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor) && cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID)
                    .ToList();

            string[] TaNameList = TaAndInstructorList
                    .Where(cu => cu.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                    .Select(cu => cu.UserProfile.FirstName + " " + cu.UserProfile.LastName)
                    .ToArray();

            int[] TaIdList = TaAndInstructorList
                    .Where(cu => cu.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                    .Select(cu => cu.UserProfileID)
                    .ToArray();

            string[] InstructorNameList = TaAndInstructorList
                    .Where(cu => cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                    .Select(cu => cu.UserProfile.FirstName + " " + cu.UserProfile.LastName)
                    .ToArray();

            int[] InstructorIdList = TaAndInstructorList
                    .Where(cu => cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                    .Select(cu => cu.UserProfileID)
                    .ToArray();

            if (InitialRecipients != null)
            {
                string[] RecipientNameList = InitialRecipients
                        .Select(up => up.FirstName + " " + up.LastName)
                        .ToArray();

                int[] RecipientIdList = InitialRecipients
                        .Select(up => up.ID)
                        .ToArray();

                ViewBag.RecipientNameList = string.Join(",", RecipientNameList);
                ViewBag.RecipientIdList = string.Join(",", RecipientIdList);
            }

            ViewBag.MailHeader = "New Message";
            ViewBag.TaNameList = string.Join(",", TaNameList);
            ViewBag.TaIdList = string.Join(",", TaIdList);
            ViewBag.InstructorNameList = string.Join(",", InstructorNameList);
            ViewBag.InstructorIdList = string.Join(",", InstructorIdList);
            
        }

        public ActionResult Create()
        {
            setUpMailViewBags();
            return View("Create", new Mail());
        }

        [HttpPost]
        public ActionResult Create(Mail mail)
        {
            if (ModelState.IsValid)
            {
                string recipient_string = Request.Params["recipientlist"];
                string[] recipients;

                // gets the current courseid
                mail.ContextID = ActiveCourseUser.AbstractCourseID;

                // gets the current course
                mail.Context = db.Courses.Where(b => b.ID == mail.ContextID).FirstOrDefault();

                if (recipient_string != null)
                {
                    recipients = recipient_string.Split(',');
                    int count = 0;
                    int threadID = 0;
                    int dummyOut = 0;

                    foreach (string id in recipients)
                    {
                        if (Int32.TryParse(id, out dummyOut))
                        {
                            Mail newMail = new Mail();
                            newMail.FromUserProfileID = CurrentUser.ID;
                            newMail.Read = false;
                            newMail.ToUserProfileID = Convert.ToInt32(id);
                            newMail.Subject = mail.Subject;
                            newMail.Message = mail.Message;
                            newMail.ThreadID = threadID;
                            newMail.ContextID = mail.ContextID;
                            newMail.DeleteFromInbox = false;
                            newMail.DeleteFromOutbox = false;

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
                    }
                    return RedirectToAction("Index");
                }
            }
            return View(mail);
        }
        
        public ActionResult CreateUser(int courseUserId)
        {
            List<UserProfile> recipientList = new List<UserProfile>();

            CourseUser studentRec = db.CourseUsers.Find(courseUserId);
            if (studentRec != null)
            {
                recipientList.Add(studentRec.UserProfile);
            }

            setUpMailViewBags(recipientList);
            return View("Create", new Mail());
        }

        public ActionResult CreateTeam(int teamID)
        {
            ViewBag.MailHeader = "New Team Message";

            List<UserProfile> recipientList = new List<UserProfile>();

            Team team = db.Teams.Find(teamID);
            foreach (TeamMember tm in team.TeamMembers)
            {
                if (tm.CourseUserID != ActiveCourseUser.ID)
                {
                    recipientList.Add(tm.CourseUser.UserProfile);
                }
            }

            setUpMailViewBags(recipientList);
            return View("Create", new Mail());
        }

        public ActionResult CreateDiscussionTeam(int discussionTeamId)
        {
            ViewBag.MailHeader = "New Message";
            List<UserProfile> recipientList = new List<UserProfile>();

            DiscussionTeam discussionTeam = db.DiscussionTeams.Find(discussionTeamId);
            foreach (TeamMember tm in discussionTeam.GetAllTeamMembers())
            {
                if (tm.CourseUserID != ActiveCourseUser.ID)
                {
                    recipientList.Add(tm.CourseUser.UserProfile);
                }
            }

            setUpMailViewBags(recipientList);
            return View("Create", new Mail());
        }

        public ActionResult CreateReplyAll()
        {
            int replyto;
            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();
            string mailHeader = "";
            if (Int32.TryParse(Request.Params["replyAll"], out replyto) == true)
            {
                if ((mail = db.Mails.Find(replyto)) != null)
                {
                    recipientList = (from m in db.Mails
                                     where m.ThreadID == replyto &&
                                     m.ToUserProfileID != CurrentUser.ID
                                     select m.ToUserProfile).ToList<UserProfile>();
                    if (!recipientList.Contains(mail.FromUserProfile))
                    {
                        recipientList.Add(mail.FromUserProfile); //Adds the sender to the reply list
                    }
                    mailHeader = mail.Subject = "RE: " + mail.Subject;
                    // Prefix each line with a '> '
                    mail.Message = "\n\nOriginal Message \nFrom: " + mail.FromUserProfile.FirstName + " " +
                                    mail.FromUserProfile.LastName + "\nSent at: " + mail.Posted.ToString() + "\n\n" +
                                    Regex.Replace(mail.Message, "^.*$", "> $&",
                                    RegexOptions.Multiline);
                }
            }

            setUpMailViewBags(recipientList);
            ViewBag.MailHeader = mailHeader;
            return View("Create", new Mail());
        }


        public ActionResult CreateReply()
        {
            int replyto;
            List<UserProfile> recipientList = new List<UserProfile>();
            Mail mail = new Mail();
            string mailHeader = "";
            if (Int32.TryParse(Request.Params["replyTo"], out replyto) == true)
            {
                if ((mail = db.Mails.Find(replyto)) != null)
                {
                    recipientList.Add(mail.FromUserProfile); //Adds the sender to the reply list
                    mailHeader = mail.Subject = "RE: " + mail.Subject;
                    // Prefix each line with a '> '
                    mail.Message = "\n\nOriginal Message \nFrom: " + mail.FromUserProfile.FirstName + " " +
                                    mail.FromUserProfile.LastName + "\nSent at: " + mail.Posted.ToString() + "\n\n" +
                                    Regex.Replace(mail.Message, "^.*$", "> $&",
                                    RegexOptions.Multiline);
                }
            }

            setUpMailViewBags(recipientList);
            ViewBag.MailHeader = mailHeader;
            return View("Create", new Mail());
        }

        public ActionResult CreateForward()
        {
            int forwardto;
            Mail mail = new Mail();
            string maiHeader = "";
            List<UserProfile> recipientList = new List<UserProfile>();
            if (Int32.TryParse(Request.Params["forwardTo"], out forwardto) == true)
            {
                if ((mail = db.Mails.Find(forwardto)) != null)
                {
                    maiHeader = mail.Subject = "FW: " + mail.Subject;
                    // Prefix each line with a '> '
                    mail.Message = "\n\nOriginal Message \nFrom: " + mail.FromUserProfile.FirstName + " " +
                                    mail.FromUserProfile.LastName + "\nSent at: " + mail.Posted.ToString() + "\n\n" +
                                    Regex.Replace(mail.Message, "^.*$", "> $&",
                                    RegexOptions.Multiline);
                }
            }

            setUpMailViewBags();
            ViewBag.MailHeader = maiHeader;
            return View("Create", new Mail());
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

            // If we are anonymous, limit search to ourselves plus instructors/TAs
            List<int> addedCourses = currentCourses
                .Where(c => c.AbstractRole.Anonymized == true)
                .Select(c => c.AbstractCourseID)
                .ToList();

            List<UserProfile> addedUsers = db.CourseUsers
                .Where(c => addedCourses.Contains(c.AbstractCourseID) && ((c.UserProfileID == CurrentUser.ID) || (c.AbstractRole.CanGrade == true)))
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
        [HttpPost]
        public ActionResult DeleteFromInbox(int id)
        {
            Mail mail = db.Mails.Find(id);

            if (mail.ToUserProfile.ID == CurrentUser.ID)
            {
                if (mail.DeleteFromOutbox == true)
                {
                    db.Mails.Remove(mail);
                    db.SaveChanges();
                }
                else
                {
                    mail.DeleteFromInbox = true;
                    db.SaveChanges();
                }
            }
            else
            {
                Response.StatusCode = 403;
                return View("_AjaxEmpty");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult DeleteFromOutbox(int id)
        {
            Mail mail = db.Mails.Find(id);

            if (mail.FromUserProfile.ID == CurrentUser.ID)
            {
                if (mail.DeleteFromInbox == true)
                {
                    db.Mails.Remove(mail);
                    db.SaveChanges();
                }
                else
                {
                    mail.DeleteFromOutbox = true;
                    db.SaveChanges();
                }
            }
            else
            {
                Response.StatusCode = 403;
                return View("_AjaxEmpty");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult DeleteSelectedInbox(string deleteIds)
        {
            string[] idsToDelete = deleteIds.Split(',');
            foreach (string s in idsToDelete)
            {
                Mail m = db.Mails.Find(Convert.ToInt32(s));
                if (m != null)
                {
                    if (m.ToUserProfileID == CurrentUser.ID)
                    {
                        if (m.DeleteFromOutbox == true)
                        {
                            db.Mails.Remove(m);
                        }
                        else
                        {
                            m.DeleteFromInbox = true;
                        }
                        db.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult DeleteSelectedOutbox(string deleteIds)
        {
            string[] idsToDelete = deleteIds.Split(',');
            foreach (string s in idsToDelete)
            {
                Mail m = db.Mails.Find(Convert.ToInt32(s));
                if (m != null)
                {
                    if (m.FromUserProfileID == CurrentUser.ID)
                    {
                        if (m.DeleteFromInbox == true)
                        {
                            db.Mails.Remove(m);
                        }
                        else
                        {
                            m.DeleteFromOutbox = true;
                        }
                        db.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Outbox");
        }
    }
}
