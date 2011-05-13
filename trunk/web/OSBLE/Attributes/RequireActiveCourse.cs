using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Controllers;

namespace OSBLE.Attributes
{
    /// <summary>
    /// Redirects to index if no active course is present.
    /// </summary>
    public class RequireActiveCourse : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            OSBLEController controller = filterContext.Controller as OSBLEController;
            if (!controller.HasActiveCourse())
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
            }
        }
    }
}