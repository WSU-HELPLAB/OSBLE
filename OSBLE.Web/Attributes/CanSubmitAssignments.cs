﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Controllers;

namespace OSBLE.Attributes
{
    /// <summary>
    /// Redirects to index if user cannot submit assignments
    /// </summary>
    /// 
    public class CanSubmitAssignments : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.Controller is OSBLEController)
            {
                OSBLEController controller = filterContext.Controller as OSBLEController;

                if (!controller.ActiveCourseUser.AbstractRole.CanSubmit)
                {
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" }));
                }
                
            }
        }
    }
}