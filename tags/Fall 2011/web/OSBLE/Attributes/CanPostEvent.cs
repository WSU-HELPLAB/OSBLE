using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Controllers;
using OSBLE.Models.Courses;

namespace OSBLE.Attributes
{
    /// <summary>
    /// Redirects to index if user has no modify permissions for current course.
    /// </summary>
    ///
    public class CanPostEvent : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.Controller is OSBLEController)
            {
                OSBLEController controller = filterContext.Controller as OSBLEController;

                CoursesUsers cu = controller.ActiveCourse;

                // Course allows Instructors to post events, community allows Leaders to post events.
                if (!(cu.AbstractRole.CanModify || cu.AbstractCourse.AllowEventPosting))
                {
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
                }
    
            }
        }
    }
}