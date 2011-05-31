using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models.HomePage;
using OSBLE.Models.Courses;
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

            db.Notifications.Add(n);
            db.SaveChanges();
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
                n.RecipientID = instructor.ID;
                n.SenderID = e.PosterID;

                db.Notifications.Add(n);
            }

            db.SaveChanges();
        }
    }
}