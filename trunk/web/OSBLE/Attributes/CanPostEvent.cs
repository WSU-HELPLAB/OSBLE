using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Controllers;
using OSBLE.Models;

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

                AbstractCourse ac = controller.ActiveCourse.Course;

                if (ac is Course)
                {
                    // Course allows Instructors or TAs to post events.
                    if (!(controller.ActiveCourse.CourseRole.CanGrade || controller.ActiveCourse.Course.AllowEventPosting))
                    {
                        filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
                    }

                }
                else
                { // Community

                    // Community allows only Leaders to post events.
                    if (!(controller.ActiveCourse.CourseRole.CanModify || controller.ActiveCourse.Course.AllowEventPosting))
                    {
                        filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
                    }
                }
                
            }
        }
    }
}