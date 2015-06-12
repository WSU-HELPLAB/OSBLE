using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.HomePage;
using OSBLE.Models.Assignments;
using OSBLE.Models.AbstractCourses.Course;



namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    public class MailController : OSBLEController
    {
        public MailController()
        {
            ViewBag.Cache = Cache;
        }

        public enum MailSort
        {
            LatestDate = 1,
            EarliestDate,
            Context,
            ReverseContext,
            From,
            ReverseFrom,
            Subject,
            ReverseSubject
        }

        public ViewResult Index(MailSort sortBy = MailSort.LatestDate)
        {
            ViewBag.BoxHeader = "Inbox";
            List<Mail> mails = db.Mails.Where(m => m.ToUserProfileID == CurrentUser.ID && m.DeleteFromInbox == false).ToList();
            OrderMailAndSetViewBag(sortBy, ref mails);
            return View(mails);
        }

        public ViewResult Outbox(MailSort sortBy = MailSort.LatestDate)
        {
            ViewBag.BoxHeader = "Outbox";
            List<Mail> mails = db.Mails.Where(m => m.FromUserProfileID == CurrentUser.ID && m.DeleteFromOutbox == false).ToList();
            OrderMailAndSetViewBag(sortBy, ref mails);
            return View("Index", mails);
        }

        /// <summary>
        /// This function will take a list of mails, order them by the appropriate <see cref="MailSort"/>. Additionally, it will set up the viewbags
        /// needed for the index view, based on the sortby parameter.
        /// </summary>
        /// <param name="sortBy">A MailSort that determines how the mail will be sorted</param>
        /// <param name="mails">A reference list in which to sort by sortBy</param>
        /// <returns></returns>
        private List<Mail> OrderMailAndSetViewBag(MailSort sortBy, ref List<Mail> mails)
        {
            ViewBag.DateSortByValue = MailSort.LatestDate;
            ViewBag.ContextSortByValue = MailSort.Context;
            ViewBag.FromSortByValue = MailSort.From;
            ViewBag.SubjectSortByValue = MailSort.Subject;

            if (sortBy == MailSort.LatestDate)
            {
                mails = mails.OrderByDescending(m => m.Posted).ToList();
                ViewBag.DateSortByValue = MailSort.EarliestDate;
            }
            else if (sortBy == MailSort.EarliestDate)
            {
                mails = mails.OrderBy(m => m.Posted).ToList();
            }
            else if (sortBy == MailSort.Context)
            {
                mails = mails.OrderBy(m => m.Context).ToList();
                ViewBag.ContextSortByValue = MailSort.ReverseContext;
            }
            else if (sortBy == MailSort.ReverseContext)
            {
                mails = mails.OrderByDescending(m => m.Context).ToList();
            }
            else if (sortBy == MailSort.From)
            {
                mails = mails.OrderBy(m => m.FromUserProfile.FirstName).ThenBy(m => m.FromUserProfile.LastName).ToList();
                ViewBag.FromSortByValue = MailSort.ReverseFrom;
            }
            else if (sortBy == MailSort.ReverseFrom)
            {
                mails = mails.OrderByDescending(m => m.FromUserProfile.FirstName).ThenBy(m => m.FromUserProfile.LastName).ToList();
            }
            else if (sortBy == MailSort.Subject)
            {
                mails = mails.OrderBy(m => m.Subject).ToList();
                ViewBag.SubjectSortByValue = MailSort.ReverseSubject;
            }
            else if (sortBy == MailSort.ReverseSubject)
            {
                mails = mails.OrderByDescending(m => m.Subject).ToList();
            }
            return mails;
        }

        /// <summary>
        /// View for a specific mail item. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
            List<Mail> mailItems = null;
            if (mail.ToUserProfileID == CurrentUser.ID)
            {
                mailItems = (from m in db.Mails
                             where m.ToUserProfileID == CurrentUser.ID &&
                             !m.DeleteFromInbox
                             orderby m.ID ascending
                             select m).ToList();
            }
            else if (mail.FromUserProfileID == CurrentUser.ID)
            {
                mailItems = (from m in db.Mails
                             where m.FromUserProfileID == CurrentUser.ID &&
                             !m.DeleteFromOutbox
                             orderby m.ID ascending
                             select m).ToList();
            }
            else //Mail is not to or from them, should not be viewing it!
            {
                return RedirectToAction("Index");
            }


            //MG&KD:Loops through the mail (that is sorted ascending). Continues to set and replace "prev" while the ID is lower than the current mails ID
            //Eventually if there is an ID that is higher than the current, it will be the set to "next" and break. Setting both prev and next if there are valid values.
            if (mailItems != null)
            {
                foreach (Mail m in mailItems)
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

        public void setUpWhiteTableMailViewBags(List<OSBLE.Models.AbstractCourses.Course.WhiteTableUser> InitialRecipients = null )
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
                        .Select(up => up.Name2 + " " + up.Name1)
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

        public ActionResult NoCourses()
        {
            return View();
        }

        public ActionResult Create()
        {
            try
            {
                setUpMailViewBags();
            }
            catch (Exception)
            {
                return RedirectToAction("NoCourses");
            }
            return View("Create", new Mail());
        }

        [HttpPost]
        public ActionResult Create(Mail mail)
        {
            if (ModelState.IsValid)
            {
                
                string recipient_string = Request.Params["recipientlist"];
                string[] recipients;
                string currentCourse = Request.Form["CurrentlySelectedCourse"];    //gets selected FROM courseid
                string mailReply = Request.Form["mailReply"];
                if(mailReply == "" || mailReply == null)
                {
                    mail.ContextID = Convert.ToInt16(currentCourse); 
                }
                else
                {
                    //we want the default context if it's a reply
                    mail.ContextID = ActiveCourseUser.AbstractCourseID;                    
                }
                //mail.ContextID = ActiveCourseUser.AbstractCourseID;

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

        public ActionResult CreateUserProfileId(int id)
        {
            UserProfile profile = db.UserProfiles.Find(id);
            List<UserProfile> recipientList = new List<UserProfile>();
            if (profile != null)
            {
                recipientList.Add(profile);
            }
            setUpMailViewBags(recipientList);
            return View("Create", new Mail());
        }

        public ActionResult CreateWhiteTableUserProfileId(int id)
        {
            var profile = db.WhiteTableUsers.Find(id);
            List<OSBLE.Models.AbstractCourses.Course.WhiteTableUser> recipientList 
                = new List<Models.AbstractCourses.Course.WhiteTableUser>();

            if(profile != null)
            {
                recipientList.Add(profile);
            }
            setUpWhiteTableMailViewBags(recipientList);
            return View("Create", new Mail());



        }

        public ActionResult CreateUser(int id)
        {
            List<UserProfile> recipientList = new List<UserProfile>();

            CourseUser studentRec = db.CourseUsers.Find(id);
            if (studentRec != null)
            {
                recipientList.Add(studentRec.UserProfile);
            }

            setUpMailViewBags(recipientList);
            return View("Create", new Mail());
        }

        public ActionResult CreateTeam(int teamID)
        {
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
                    mail.Subject = "RE: " + mail.Subject;
                    // Prefix each line with a '> '
                    mail.Message = "\n\nOriginal Message \nFrom: " + mail.FromUserProfile.FirstName + " " +
                                    mail.FromUserProfile.LastName + "\nSent at: " + mail.Posted.ToString() + "\n\n" +
                                    Regex.Replace(mail.Message, "^.*$", "> $&",
                                    RegexOptions.Multiline);
                }
            }

            setUpMailViewBags(recipientList);
            return View("Create", mail);
        }


        public ActionResult CreateReply()
        {
            int replyto;
            List<UserProfile> recipientList = new List<UserProfile>();
            Mail mail = new Mail();
            if (Int32.TryParse(Request.Params["replyTo"], out replyto) == true)
            {
                if ((mail = db.Mails.Find(replyto)) != null)
                {
                    recipientList.Add(mail.FromUserProfile); //Adds the sender to the reply list
                    mail.Subject = "RE: " + mail.Subject;
                    // Prefix each line with a '> '
                    mail.Message = "\n\nOriginal Message \nFrom: " + mail.FromUserProfile.FirstName + " " +
                                    mail.FromUserProfile.LastName + "\nSent at: " + mail.Posted.ToString() + "\n\n" +
                                    Regex.Replace(mail.Message, "^.*$", "> $&",
                                    RegexOptions.Multiline);
                }
            }

            setUpMailViewBags(recipientList);
            return View("Create", mail);
        }

        public ActionResult CreateForward()
        {
            int forwardto;
            Mail mail = new Mail();
            List<UserProfile> recipientList = new List<UserProfile>();
            if (Int32.TryParse(Request.Params["forwardTo"], out forwardto) == true)
            {
                if ((mail = db.Mails.Find(forwardto)) != null)
                {
                    mail.Subject = "FW: " + mail.Subject;
                    // Prefix each line with a '> '
                    mail.Message = "\n\nOriginal Message \nFrom: " + mail.FromUserProfile.FirstName + " " +
                                    mail.FromUserProfile.LastName + "\nSent at: " + mail.Posted.ToString() + "\n\n" +
                                    Regex.Replace(mail.Message, "^.*$", "> $&",
                                    RegexOptions.Multiline);
                }
            }

            setUpMailViewBags();
            return View("Create", mail);
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

        /// <summary>
        /// This is the delete used for the inbox/outbox form.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Delete()
        {
            bool fromOutbox = Request.UrlReferrer.ToString().Contains("Outbox");
            string[] keys = Request.Params.AllKeys.Where(s => s.Contains("mailItem_")).ToArray();
            List<int> mailIdsToDelete = new List<int>();
            string value = "";
            int mailId = 0;
            foreach (string key in keys)
            {
                value = Request.Params[key];
                if (value == "on") //Checkbox was checked...delete item
                {
                    if (int.TryParse(key.Split('_')[1], out mailId) == false) //couldnt get an id, bad apple - continue.
                    {
                        continue;
                    }
                    Mail mailToDelete = db.Mails.Find(mailId);
                    DeleteMail(mailToDelete, fromOutbox);
                }
            }
            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult DeleteSingle(Mail mail)
        {
            mail = db.Mails.Find(mail.ID);
            bool fromOutbox = (Request.Params["deleteFrom"] == "outbox");
            DeleteMail(mail, fromOutbox);
            if (fromOutbox)
            {
                return RedirectToAction("Outbox");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// This function handles the "deleting" of mail. Which mostly consists of switching booleans
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="fromOutbox"></param>
        public void DeleteMail(Mail mail, bool fromOutbox)
        {
            if (fromOutbox)
            {
                mail.DeleteFromOutbox = true;
            }
            else
            {
                mail.DeleteFromInbox = true;
            }

            if (mail.DeleteFromInbox == true && mail.DeleteFromOutbox == true)
            {
                db.Mails.Remove(mail);
            }
            db.SaveChanges();
        }

        [HttpGet]
        public JsonResult GetUserCourseList()
        {
            int id = this.CurrentUser.ID;

            List<CourseUser> courseUsers = new List<CourseUser>();
            courseUsers = db.CourseUsers.Where(u => u.UserProfileID == id).ToList();

            List<string> userCourseList = new List<string>();

            foreach (CourseUser cu in courseUsers)
            {
                string tag = GetCourseTags(db.AbstractCourses.Where(a => a.ID == cu.AbstractCourseID).FirstOrDefault());
                if (tag != "")
                    userCourseList.Add(cu.AbstractCourseID + "," + tag);
            }
            
            return Json(userCourseList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Returns tags for either a course or a community, if one exists for the notification. Otherwise, empty string.
        ///
        /// </summary>
        /// <param name="c">The abstract course</param>
        /// <returns>Tag with leading space (" CptS 314") if course or community exists, "" if not.</returns>
        private string GetCourseTags(AbstractCourse c)
        {
            string tag = "";

            if (c != null)
            {
                if (c is Course)
                {
                    tag = (c as Course).Prefix + " " + (c as Course).Number;
                }
                else if (c is Community)
                {
                    tag = (c as Community).Nickname;
                }
            }

            return tag;
        }
    }
}
