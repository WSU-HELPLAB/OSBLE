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
    /// Redirects to index if user is not an admin
    /// </summary>
    /// 
    public class IsAdmin : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.Controller is OSBLEController)
            {
                OSBLEController controller = filterContext.Controller as OSBLEController;

                if ((controller.CurrentUser == null) || (!controller.CurrentUser.IsAdmin))
                {
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
                }
            }
        }
    }
}