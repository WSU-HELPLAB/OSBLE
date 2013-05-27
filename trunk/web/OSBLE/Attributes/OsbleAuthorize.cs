using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Controllers;
using System.Web.Routing;
using System.Web.Security;
using OSBLE.Utility;
using System.Configuration;
using OSBLE.Models.Users;
using OSBLE.Models;

namespace OSBLE.Attributes
{
    /// <summary>
    /// Written to replace the default .NET authorize attribute as our shared hosting will time it out after an hour (sometimes much less).
    /// </summary>
    public class OsbleAuthorize : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (OsbleAuthentication.CurrentUser == null)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Account", action = "LogOn", returnUrl = filterContext.HttpContext.Request.Url }));
            }
        }
    }
}