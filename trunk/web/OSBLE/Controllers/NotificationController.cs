using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models.HomePage;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using System.Net.Mail;
using System.Configuration;

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
        /// Creates and posts a new request for approval on an event posting and sends to all instructors in a course
        /// </summary>
        /// <param name="e">The event that requires approval</param>
        /// <param name="db">The current database context</param>
        [NonAction]
        public void SendEventApprovalNotification(Event e)
        {
            // Get all instructors in the course.
            List<UserProfile> instructors = db.CoursesUsers.Where(c => (c.CourseID == e.CourseID)
                && (c.CourseRoleID == (int)CourseRole.OSBLERoles.Instructor))
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
            if (n.Recipient.EmailAllNotifications)
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
            }
            
            body += "\n\n---\nDo not reply to this email.\nVisit this link to " + action + ": " + getDispatchURL(n.ID);

            MailMessage message = new MailMessage(new MailAddress(ConfigurationSettings.AppSettings["OSBLEFromEmail"], "OSBLE"), 
                                new MailAddress(recipient.UserName,recipient.FirstName + " " + recipient.LastName));

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