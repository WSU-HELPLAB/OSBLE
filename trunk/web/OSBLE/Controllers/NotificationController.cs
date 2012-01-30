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
using OSBLE.Models.Assignments;

namespace OSBLE.Controllers
{
    [Authorize]
    public class NotificationController : OSBLEController
    {
        //
        // GET: /Notification/
        public ActionResult Index()
        {
            ViewBag.Notifications = db.Notifications.Where(n => (n.RecipientID == activeCourse.ID)).OrderByDescending(n => n.Posted).ToList();
            return View();
        }

        public ActionResult Dispatch(int id)
        {
            Notification n = db.Notifications.Find(id);

            // Notification exists and belongs to current user.
            if ((n != null) && (n.RecipientID == activeCourse.ID))
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
                    case Notification.Types.FileSubmitted:
                        return RedirectToAction("getCurrentUsersZip", "FileHandler", new { assignmentID = n.ItemID });
                    case Notification.Types.InlineReviewCompleted:
                        return RedirectToAction("Details", "PeerReview", new { ID = n.ItemID });
                    case Notification.Types.RubricEvaluationCompleted:
                        return RedirectToAction("View", "Rubric", new { assignmentId = n.ItemID, teamId = n.SenderID });
                }
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult MarkAsRead(int id)
        {
            Notification n = db.Notifications.Find(id);

            // Notification exists and belongs to current user.
            if ((n != null) && (n.RecipientID == activeCourse.ID))
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
            n.RecipientID = db.CourseUsers.Where(cu => cu.UserProfileID == mail.ToUserProfileID).FirstOrDefault().ID;
            n.SenderID = db.CourseUsers.Where(cu => cu.UserProfileID == mail.FromUserProfileID).FirstOrDefault().ID;
            addNotification(n);
        }

        /// <summary>
        /// Posts a notification when others have participated in a dashboard thread which you have participated in
        /// </summary>
        /// <param name="dp">The parent dashboard post</param>
        /// <param name="poster">The user profile of the user who posted the reply</param>
        [NonAction]
        public void SendDashboardNotification(DashboardPost dp, CourseUser poster)
        {
            List<CourseUser> sendToUsers = new List<CourseUser>();

            // Send notification to original thread poster if poster is not anonymized,
            // are still in the course,
            // and are not the poster of the new reply.
            if (poster != null && !poster.AbstractRole.Anonymized && dp.CourseUser.UserProfileID != poster.ID)
            {
                sendToUsers.Add(dp.CourseUser);
            }

            foreach (DashboardReply reply in dp.Replies)
            {
                // Send notifications to each participant as long as they are not anonymized,
                // are still in the course,
                // and are not the poster of the new reply.
                // Also checks to make sure a duplicate notification is not sent.
                if (reply.CourseUser != null && !reply.CourseUser.AbstractRole.Anonymized && reply.CourseUserID != poster.ID && !sendToUsers.Contains(reply.CourseUser))
                {
                    sendToUsers.Add(reply.CourseUser);
                }
            }

            // Send notification to each valid user.
            foreach (CourseUser courseUser in sendToUsers)
            {
                //ignore null course users
                if (courseUser == null)
                {
                    continue;
                }
                Notification n = new Notification();
                n.ItemType = Notification.Types.Dashboard;
                n.ItemID = dp.ID;
                n.RecipientID = courseUser.ID;
                n.SenderID = poster.ID;
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
            List<CourseUser> instructors = (from i in db.CourseUsers
                                             where
                                                 i.AbstractCourseID == e.Poster.AbstractCourseID &&
                                                 i.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor
                                             select i).ToList();
                
            foreach (CourseUser instructor in instructors)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.EventApproval;
                n.ItemID = e.ID;
                n.RecipientID = instructor.ID;
                n.SenderID = e.Poster.ID;

                addNotification(n);
            }
        }

        [NonAction]
        public void SendInlineReviewCompletedNotification(Assignment assignment, AssignmentTeam team)
        {
            List<CourseUser> users = GetAllCourseUsers(team);

            foreach (CourseUser user in users)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.InlineReviewCompleted;
                n.Data = assignment.ID.ToString() + ";" + team.TeamID.ToString() + ";" + assignment.AssignmentName;
                n.RecipientID = user.ID;
                n.SenderID = activeCourse.ID;
                addNotification(n);
            }
        }

        [NonAction]
        public void SendRubricEvaluationCompletedNotification(Assignment assignment, AssignmentTeam team)
        {
            List<CourseUser> users = GetAllCourseUsers(team);

            foreach (CourseUser user in users)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.RubricEvaluationCompleted;
                n.Data = assignment.ID.ToString() + ";" + team.TeamID.ToString() + ";" + assignment.AssignmentName;
                
                n.RecipientID = user.ID;
                n.SenderID = activeCourse.ID;
                addNotification(n);
            }
        }

        [NonAction]
        public void SendFilesSubmittedNotification(Assignment assignment, AssignmentTeam team, string fileName)
        {
            if (assignment.Category.CourseID == activeCourse.AbstractCourseID)
            {
                var canGrade = (from c in db.CourseUsers
                                where c.AbstractCourseID == activeCourse.AbstractCourseID
                                && c.AbstractRole.CanGrade
                                select c).ToList();

                foreach (CourseUser user in canGrade)
                {
                    Notification n = new Notification();
                    n.ItemType = Notification.Types.FileSubmitted;
                    n.Data = assignment.ID.ToString() + ";" + team.TeamID.ToString() + ";" + assignment.AssignmentName + ";" + team.Team.Name + ";" + fileName + ";" + DateTime.Now;
                    n.RecipientID = user.ID;
                    n.SenderID = activeCourse.ID;
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
            UserProfile recipient = (from a in db.CourseUsers
                                where a.ID == n.RecipientID
                                select a.UserProfile).FirstOrDefault();
            if (recipient.EmailAllNotifications && !(recipient.EmailAllActivityPosts && n.ItemType == Notification.Types.Dashboard))
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
            
            UserProfile sender = db.UserProfiles.Find(n.Sender.UserProfileID);
            UserProfile recipient = db.UserProfiles.Find(n.Recipient.UserProfileID);

            // this comes back as null, for some reason.
            AbstractCourse course = db.AbstractCourses.Where(b => b.ID == n.CourseID).FirstOrDefault();

            string subject = "";
            if(getCourseTag(course) != "")
            {
                subject = "[" + getCourseTag(course) + "] "; // Email subject prefix
            }

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

                    body = sender.FirstName + " " + sender.LastName + " has requested your approval of an event posting.";

                    action = "approve/reject this event.";

                    break;
                case Notification.Types.Dashboard:
                    subject += "Activity Feed Reply from " + sender.FirstName + " " + sender.LastName;

                    body = sender.FirstName + " " + sender.LastName + " has posted to an activity feed thread in which you have participated.";

                    action = "view this activity feed thread.";

                    break;
                case Notification.Types.FileSubmitted:
                    subject += "New Assignment Submmission from " + sender.FirstName + " " + sender.LastName;

                    body = n.Data; //sender.FirstName + " " + sender.LastName + " has submitted an assignment."; //Can we get name of assignment?

                    action = "view this assignment submission.";

                    break;
                case Notification.Types.RubricEvaluationCompleted:
                    subject += sender.FirstName + " " + sender.LastName + "has published a rubric  Assignment Submmission from ";

                    body = n.Data; //sender.FirstName + " " + sender.LastName + " has submitted an assignment."; //Can we get name of assignment?

                    action = "view this assignment submission.";

                    break;
                case Notification.Types.InlineReviewCompleted:
                    subject += sender.FirstName + " " + sender.LastName + "has published a rubric  Assignment Submmission from ";

                    body = n.Data; //sender.FirstName + " " + sender.LastName + " has submitted an assignment."; //Can we get name of assignment?

                    action = "view this assignment submission.";

                    break;
                default:
                    subject += "No Email set up for this type of notification";

                    body = "No Email set up for this type of notification of type: " + n.ItemType;
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
                    tag = (c as Course).Prefix + " " + (c as Course).Number;
                }
                else if (c is Community)
                {
                    tag = (c as Community).Nickname;
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