using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    [Authorize]
    public class NotificationController : OSBLEController
    {
        //
        // GET: /Notification/
        public ActionResult Index()
        {
            ViewBag.Notifications = db.Notifications.Where(n => n.RecipientID == currentUser.ID).OrderByDescending(n=>n.Posted).ToList();
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

    }
}
