using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;

namespace OSBLE.Controllers
{
    [Authorize]
    public class NotificationController : OSBLEController
    {
        //
        // GET: /Notification/
        public ActionResult Index()
        {
            ViewBag.Notifications = db.Notifications.Where(n => n.RecipientID == currentUser.ID).OrderByDescending(n => n.Posted).ToList();
            return View();
        }

        public ActionResult Dispatch(int id)
        {
            Notification n = db.Notifications.Find(id);

            // Notification exists and belongs to current user.
            if ((n != null) && (n.RecipientID == currentUser.ID))
            {
                // Mark notification as read.
                n.Read = true;
                db.SaveChanges();

                // Determine which item type and dispatch to the appropriate action/controller.
                switch (n.ItemType)
                {
                    case Notification.Types.Mail:
                        return RedirectToAction("View", "Mail", new { ID = n.ItemID });
                    case Notification.Types.EventApproval:
                        return RedirectToAction("Approval", "Event", new { ID = n.ItemID });
                    case Notification.Types.Dashboard:
                        return RedirectToAction("ViewThread", "Home", new { ID = n.ItemID });
                }
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult MarkAsRead(int id)
        {
            Notification n = db.Notifications.Find(id);

            // Notification exists and belongs to current user.
            if ((n != null) && (n.RecipientID == currentUser.ID))
            {
                // Mark notification as read.
                n.Read = true;
                db.SaveChanges();
            }
            else
            {
                // Return forbidden.
                Response.StatusCode = 403;
            }

            return View("_AjaxEmpty");
        }

        /// <summary>
        /// Creates and posts a new mail message notification to its recipient
        /// </summary>
        /// <param name="mail">The message that has been sent</param>
        /// <param name="db">The current database context</param>
        [NonAction]
        public void SendMailNotification(Mail mail)
        {
            // Send notification to recipient about new message.
            Notification n = new Notification();
            n.ItemType = Notification.Types.Mail;
            n.ItemID = mail.ID;
            n.RecipientID = mail.ToUserProfileID;
            n.SenderID = mail.FromUserProfileID;

            addNotification(n);
        }

        /// <summary>
        /// Posts a notification when others have participated in a dashboard thread which you have participated in
        /// </summary>
        /// <param name="dp">The parent dashboard post</param>
        /// <param name="poster">The user profile of the user who posted the reply</param>
        [NonAction]
        public void SendDashboardNotification(DashboardPost dp, UserProfile poster)
        {
            List<int> sendToUsers = new List<int>();

            CoursesUsers dpPosterCu = db.CoursesUsers.Where(cu => cu.AbstractCourseID == dp.CourseID && cu.UserProfileID == dp.UserProfileID).FirstOrDefault();

            // Send notification to original thread poster if poster is not anonymized,
            // are still in the course,
            // and are not the poster of the new reply.
            if (dpPosterCu != null && !dpPosterCu.AbstractRole.Anonymized && dp.UserProfileID != poster.ID)
            {
                sendToUsers.Add(dp.UserProfileID);
            }

            foreach (DashboardReply dr in dp.Replies)
            {
                CoursesUsers drPosterCu = db.CoursesUsers.Where(cu => cu.AbstractCourseID == dp.CourseID && cu.UserProfileID == dr.UserProfileID).FirstOrDefault();

                // Send notifications to each participant as long as they are not anonymized,
                // are still in the course,
                // and are not the poster of the new reply.
                // Also checks to make sure a duplicate notification is not sent.
                if (drPosterCu != null && !drPosterCu.AbstractRole.Anonymized && dr.UserProfileID != poster.ID && !sendToUsers.Contains(dr.UserProfileID))
                {
                    sendToUsers.Add(dr.UserProfileID);
                }
            }

            // Send notification to each valid user.
            foreach (int UserProfileID in sendToUsers)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.Dashboard;
                n.ItemID = dp.ID;
                n.RecipientID = UserProfileID;
                n.SenderID = poster.ID;
                n.CourseID = dp.CourseID;

                addNotification(n);
            }
        }

        /// <summary>
        /// Creates and posts a new request for approval on an event posting and sends to all instructors in a course
        /// </summary>
        /// <param name="e">The event that requires approval</param>
        /// <param name="db">The current database context</param>
        [NonAction]
        public void SendEventApprovalNotification(Event e)
        {
            // Get all instructors in the course.
            List<UserProfile> instructors = db.CoursesUsers.Where(c => (c.AbstractCourseID == e.CourseID)
                && (c.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor))
                .Select(c => c.UserProfile).ToList();

            foreach (UserProfile instructor in instructors)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.EventApproval;
                n.ItemID = e.ID;
                n.CourseID = e.CourseID;
                n.RecipientID = instructor.ID;
                n.SenderID = e.PosterID;

                addNotification(n);
            }
        }

        [NonAction]
        public void SendInlineReviewCompletedNotification(AbstractAssignmentActivity activity, TeamUserMember teamUserMember)
        {
            List<UserProfile> users = GetAllUsers(teamUserMember);

            foreach (UserProfile user in users)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.InlineReviewCompleted;
                n.Data = activity.ID.ToString() + ";" + teamUserMember.ID.ToString() + ";" + activity.Name;
                n.RecipientID = user.ID;
                n.SenderID = currentUser.ID;

                addNotification(n);
            }
        }

        [NonAction]
        public void SendRubricEvaluationCompletedNotification(AbstractAssignmentActivity activity, TeamUserMember teamUserMember)
        {
            List<UserProfile> users = GetAllUsers(teamUserMember);

            foreach (UserProfile user in users)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.RubricEvaluationCompleted;
                n.Data = activity.ID.ToString() + ";" + teamUserMember.ID.ToString() + ";" + activity.Name;
                n.RecipientID = user.ID;
                n.SenderID = currentUser.ID;

                addNotification(n);
            }
        }

        [NonAction]
        public void SendFilesSubmittedNotification(AbstractAssignmentActivity activity, TeamUserMember teamUser, string fileName)
        {
            if (activity.AbstractAssignment.Category.CourseID == activeCourse.AbstractCourseID)
            {
                var canGrade = (from c in db.CoursesUsers
                                where c.AbstractCourseID == activeCourse.AbstractCourseID
                                && c.AbstractRole.CanGrade
                                select c.UserProfile).ToList();

                foreach (UserProfile user in canGrade)
                {
                    Notification n = new Notification();
                    n.ItemType = Notification.Types.FileSubmitted;
                    n.Data = activity.ID.ToString() + ";" + teamUser.ID.ToString() + ";" + activity.Name + ";" + teamUser.Name + ";" + fileName + ";" + DateTime.Now;
                    n.RecipientID = user.ID;
                    n.SenderID = currentUser.ID;

                    addNotification(n);
                }
            }
        }

        /// <summary>
        /// Adds notification to the db,
        /// and calls emailNotification if recipient's
        /// settings request notification emails.
        /// </summary>
        /// <param name="n">Notification to be added</param>
        private void addNotification(Notification n)
        {
            db.Notifications.Add(n);
            db.SaveChanges();

            // Find recipient profile and check notification settings
            UserProfile recipient = db.UserProfiles.Find(n.RecipientID);
            if (n.Recipient.EmailAllNotifications && !n.Recipient.EmailAllActivityPosts)
            {
                emailNotification(n);
            }
        }

        /// <summary>
        /// Sends an email notification to a user.
        /// Does not run in debug mode.
        /// </summary>
        /// <param name="n">Notification to be emailed</param>
        private void emailNotification(Notification n)
        {
#if !DEBUG
            SmtpClient mailClient = new SmtpClient();
            mailClient.UseDefaultCredentials = true;

            UserProfile sender = db.UserProfiles.Find(n.SenderID);
            UserProfile recipient = db.UserProfiles.Find(n.RecipientID);

            AbstractCourse course = db.AbstractCourses.Find(n.CourseID);

            string subject = "[OSBLE" + getCourseTag(course) + "] "; // Email subject prefix

            string body = "";

            string action = "";

            switch (n.ItemType)
            {
                case Notification.Types.Mail:
                    Mail m = db.Mails.Find(n.ItemID);

                    subject += "New private message from " + sender.FirstName + " " + sender.LastName;

                    action = "reply to this message";

                    body = sender.FirstName + " " + sender.LastName + " sent this message at " + m.Posted.ToString() + ":\n\n";
                    body += "Subject: " + m.Subject + "\n\n";
                    body += m.Message;

                    break;
                case Notification.Types.EventApproval:
                    subject += "Event approval request from " + sender.FirstName + " " + sender.LastName;

                    body = sender.FirstName + " " + sender.LastName + " is requesting a course event posting to be approved.";

                    action = "approve/reject this event.";

                    break;
                case Notification.Types.Dashboard:
                    subject += "Dashboard Message Reply from " + sender.FirstName + " " + sender.LastName;

                    body = sender.FirstName + " " + sender.LastName + " has posted in a dashboard thread that you have participated in.";

                    action = "view this dashboard thread.";

                    break;
            }

            body += "\n\n---\nDo not reply to this email.\nVisit this link to " + action + ": " + getDispatchURL(n.ID);

            MailMessage message = new MailMessage(new MailAddress(ConfigurationManager.AppSettings["OSBLEFromEmail"], "OSBLE"),
                                new MailAddress(recipient.UserName, recipient.FirstName + " " + recipient.LastName));

            message.Subject = subject;
            message.Body = body;

            mailClient.Send(message);

#endif
        }

        /// <summary>
        /// Returns tags for either a course or a community, if one exists for the notification. Otherwise, empty string.
        ///
        /// </summary>
        /// <param name="c">The abstract course</param>
        /// <returns>Tag with leading space (" CptS 314") if course or community exists, "" if not.</returns>
        private string getCourseTag(AbstractCourse c)
        {
            string tag = "";

            if (c != null)
            {
                if (c is Course)
                {
                    tag = " " + (c as Course).Prefix + " " + (c as Course).Number;
                }
                else if (c is Community)
                {
                    tag = " " + (c as Community).Nickname;
                }
            }

            return tag;
        }

        /// <summary>
        /// Used to get URL to append to email notifications.
        /// Based on current host URL requested by the client, so it should work on
        /// any deployment of OSBLE.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string getDispatchURL(int id)
        {
            return context.Request.Url.Scheme + System.Uri.SchemeDelimiter + context.Request.Url.Host + ((context.Request.Url.Port != 80 && context.Request.Url.Port != 443) ? ":" + context.Request.Url.Port : "") + "/Notification/Dispatch/" + id;
        }
    }
}