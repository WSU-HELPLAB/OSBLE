using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Controllers;
using OSBLE.Models.Courses;
using System;
using OSBLE.Models;

namespace OSBLE.Attributes
{
    /// <summary>
    /// Redirects to index if user has no modify permissions for current course.
    /// </summary>
    ///
    public class NotForCommunity : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.Controller is OSBLEController)
            {
                OSBLEController controller = filterContext.Controller as OSBLEController;

                //AC: Will fail when session clears
                try
                {
                    if (controller.ActiveCourse.AbstractCourse is Community)
                    {
                        filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
                    }
                }
                catch (Exception ex)
                {
                    OSBLEContext db = new OSBLEContext();
                    ActivityLog log = new ActivityLog()
                    {
                        Sender = typeof(NotForCommunity).ToString(),
                        Message = "IP " + filterContext.HttpContext.Request.UserHostAddress + " encountered exception: " + ex.ToString()
                    };
                    db.ActivityLogs.Add(log);
                    db.SaveChanges();
                }
            }
        }
    }
}