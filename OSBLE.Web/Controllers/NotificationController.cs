using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;
using System.Net.Mail;
using System.Configuration;
using OSBLE.Attributes;
using OSBLE.Utility;
using System;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    public class NotificationController : OSBLEController
    {
        //
        // GET: /Notification/
        public ActionResult Index()
        {
            ViewBag.Notifications = db.Notifications.Where(n => (n.RecipientID == ActiveCourseUser.ID)).OrderByDescending(n => n.Posted).ToList();
            return View();
        }

        public ActionResult Dispatch(int id)
        {
            Notification n = db.Notifications.Find(id);

            // Notification exists and belongs to current user.
            if ((n != null) && (n.RecipientID == ActiveCourseUser.ID))
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
                    case Notification.Types.TeamEvaluationDiscrepancy:
                        //n.Data = PrecedingTeamId + ";" + TeamEvaluationAssignment.ID;
                        int precteamId = 0;
                        int teamEvalAssignmnetID = 0;
                        int.TryParse(n.Data.Split(';')[0], out precteamId);
                        int.TryParse(n.Data.Split(';')[1], out teamEvalAssignmnetID);
                        return RedirectToAction("TeacherTeamEvaluation", "Assignment", new { precedingTeamId = precteamId, TeamEvaluationAssignmentId = teamEvalAssignmnetID });
                    case Notification.Types.JoinCourseApproval:
                        return RedirectToAction("Index", "Roster", new { ID = n.ItemID });
                    case Notification.Types.JoinCommunityApproval:
                        return RedirectToAction("Index", "Roster", new { ID = n.ItemID });
                    

                }
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult MarkAsRead(int id)
        {
            Notification n = db.Notifications.Find(id);

            // Notification exists and belongs to current user.
            if ((n != null) && (n.RecipientID == ActiveCourseUser.ID))
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

        [HttpPost]
        public ActionResult MarkAllAsRead()
        {
            List<Notification> allUnreadNotifications = (from n in db.Notifications
                                                         where n.RecipientID == ActiveCourseUser.ID &&
                                                         !n.Read
                                                         select n).ToList();

            foreach (Notification n in allUnreadNotifications)
            {
                // Mark notification as read.
                n.Read = true;
                db.SaveChanges();
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
            
            //using the data variable to store context id for email context later.
            n.Data = mail.ContextID.ToString();

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
            if (poster != null && !poster.AbstractRole.Anonymized && dp.CourseUserID != poster.ID)
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
        public void SendCourseApprovalNotification(Course c, CourseUser sender)
        {
            // Get all instructors in the course.
            List<CourseUser> instructors = (from i in db.CourseUsers
                                           where
                                             i.AbstractCourseID == c.ID &&
                                             i.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor
                                           select i).ToList();

            foreach (CourseUser instructor in instructors)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.JoinCourseApproval;
                n.ItemID = c.ID;
                n.RecipientID = instructor.ID;
                n.SenderID = sender.ID;
                
                addNotification(n);
            }
        }

        [NonAction]
        public void SendCommunityApprovalNotification(Community c, CourseUser sender)
        {
            List<CourseUser> leaders = (from i in db.CourseUsers
                                        where
                                          i.AbstractCourseID == c.ID &&
                                          i.AbstractRoleID == (int)CommunityRole.OSBLERoles.Leader
                                        select i).ToList();

            foreach (CourseUser leader in leaders)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.JoinCommunityApproval;
                n.ItemID = c.ID;
                n.RecipientID = leader.ID;
                n.SenderID = sender.ID;

                addNotification(n);
            }
        }

        [NonAction]
        public void SendInlineReviewCompletedNotification(Assignment assignment, Team team)
        {
            foreach (TeamMember member in team.TeamMembers)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.InlineReviewCompleted;
                n.Data = assignment.ID.ToString() + ";" + team.ID.ToString() + ";" + assignment.AssignmentName;
                n.RecipientID = member.CourseUserID;
                n.SenderID = ActiveCourseUser.ID;
                addNotification(n);
            }
        }

        [NonAction]
        public void SendRubricEvaluationCompletedNotification(Assignment assignment, Team team)
        {
            foreach (TeamMember member in team.TeamMembers)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.RubricEvaluationCompleted;
                n.Data = assignment.ID.ToString() + ";" + member.CourseUserID + ";" + assignment.AssignmentName;

                n.RecipientID = member.CourseUser.ID;
                n.SenderID = ActiveCourseUser.ID;
                addNotification(n);
            }
        }

        /// <summary>
        /// Sends a notification saying that the ActiveCourseUser submitted a Team Evaluation with a large percent spread.
        /// </summary>
        /// <param name="TeamEvaluationAssignmentId"></param>
        /// <param name="PrecedingTeamId"></param>
        [NonAction]
        public void SendTeamEvaluationDiscrepancyNotification(int PrecedingTeamId, Assignment TeamEvaluationAssignment)
        {
            //Sender has completed a [url]Team Evaluation with a large percent spread.

            List<int> InstructorIDs = (from cu in db.CourseUsers
                                     where cu.AbstractCourseID == TeamEvaluationAssignment.Course.ID &&
                                     cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor
                                     select cu.ID).ToList();

            foreach (int cuID in InstructorIDs)
            {
                Notification n = new Notification();
                n.ItemType = Notification.Types.TeamEvaluationDiscrepancy;
                n.Data = PrecedingTeamId + ";" + TeamEvaluationAssignment.ID;
                n.RecipientID = cuID;
                n.SenderID = ActiveCourseUser.ID;
                addNotification(n);
            }
        }

        [NonAction]
        public void SendFilesSubmittedNotification(Assignment assignment, AssignmentTeam team, string fileName)
        {
            //if (assignment.Category.CourseID == activeCourse.AbstractCourseID)
            //{
            //    var canGrade = (from c in db.CourseUsers
            //                    where c.AbstractCourseID == activeCourse.AbstractCourseID
            //                    && c.AbstractRole.CanGrade
            //                    select c).ToList();

            //    foreach (CourseUser user in canGrade)
            //    {
            //        Notification n = new Notification();
            //        n.ItemType = Notification.Types.FileSubmitted;
            //        n.Data = assignment.ID.ToString() + ";" + team.TeamID.ToString() + ";" + assignment.AssignmentName + ";" + team.Team.Name + ";" + fileName + ";" + DateTime.UtcNow;
            //        n.RecipientID = user.ID;
            //        n.SenderID = activeCourse.ID;
            //        addNotification(n);
            //    }
            //}
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
            CourseUser recipient = (from a in db.CourseUsers
                                    where a.ID == n.RecipientID
                                    select a).FirstOrDefault();
            if (recipient.UserProfile.EmailAllNotifications && !(recipient.UserProfile.EmailAllActivityPosts && n.ItemType == Notification.Types.Dashboard))
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

            SmtpClient mailClient = new SmtpClient();
            mailClient.UseDefaultCredentials = true;
            
            UserProfile sender = db.UserProfiles.Find(n.Sender.UserProfileID);
            UserProfile recipient = db.UserProfiles.Find(n.Recipient.UserProfileID);

            // this comes back as null, for some reason. //dmo:6/5/2014 does it really? it seems to work??
            //Abstract course can represent a course or a community 
            AbstractCourse course = db.AbstractCourses.Where(b => b.ID == n.Sender.AbstractCourseID).FirstOrDefault();
            string[] temp;
            //checking to see if there is no data besides abstractCourseID
            if(n.Data != null)
            {
                temp = n.Data.Split(';');
            }
            else
            {
                temp = new string[0];
            }
            
            int id;
            
            if (temp.Length == 1) //data not being used by other mail method, send from selected course
            {
                id = Convert.ToInt16(temp[0]);
                course = db.AbstractCourses.Where(b => b.ID == id).FirstOrDefault();
            }

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
                case Notification.Types.TeamEvaluationDiscrepancy:
                    subject += sender.FirstName + " " + sender.LastName + "has submitted a Team Evaluation that has raised a discrepancy flag";

                    body = sender.FirstName + " " + sender.LastName + " has submitted a Team Evaluation that has raised a discrepancy flag."; //Can we get name of assignment?

                    action = "view team evaluation discrepancy.";

                    break;
                case Notification.Types.JoinCourseApproval:
                    subject += sender.FirstName + " " + sender.LastName + "has submitted a request to join" + course.Name;

                    body = sender.FirstName + " " + sender.LastName + " has submitted a request to join" + course.Name; 

                    action = "view the request to join.";

                    break;
                case Notification.Types.JoinCommunityApproval:
                    subject += sender.FirstName + " " + sender.LastName + "has submitted a request to join" + course.Name;

                    body = sender.FirstName + " " + sender.LastName + " has submitted a request to join" + course.Name; 

                    action = "view the request to join.";

                    break;
                default:
                    subject += "No Email set up for this type of notification";

                    body = "No Email set up for this type of notification of type: " + n.ItemType;
                    break;
            }

            body += "\n\n---\nDo not reply to this email.\nVisit this link to " + action + ": " + getDispatchURL(n.ID);
            MailAddress to = new MailAddress(recipient.UserName, recipient.DisplayName((int)CourseRole.CourseRoles.Instructor));
            List<MailAddress> recipients = new List<MailAddress>();
            recipients.Add(to);
            Email.Send(subject, body, recipients);
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