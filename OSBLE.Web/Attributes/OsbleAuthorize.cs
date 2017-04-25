using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Ajax.Utilities;
using OSBLE.Utility;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLE.Attributes
{
    /// <summary>
    /// Written to replace the default .NET authorize attribute as our shared hosting will time it out after an hour (sometimes much less).
    /// </summary>
    public class OsbleAuthorize : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var a = HttpContext.Current.Server.MapPath("~").TrimEnd('\\');
            var path = string.Format(Directory.GetParent(a).FullName);

            var auth =
                new Authentication(path);

            var key = auth.GetAuthenticationKey();

            //were we supplied an authentication key from the query string?
            if (filterContext.HttpContext != null)
            {
                var authQueryKey = filterContext.HttpContext.Request.QueryString["auth"];
                if (!authQueryKey.IsNullOrWhiteSpace())
                {
                    key = authQueryKey;

                    //if the key is valid, log the user into the system and then retry the request
                    if (auth.IsValidKey(key))
                    {
                        auth.LogIn(auth.GetActiveUser(key));
                        var routeValues = new RouteValueDictionary();
                        routeValues["controller"] = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                        routeValues["action"] = filterContext.ActionDescriptor.ActionName;
                        foreach (var parameterKey in filterContext.ActionParameters.Keys)
                        {
                            var parameterValue = filterContext.ActionParameters[parameterKey];
                            routeValues[parameterKey] = parameterValue;
                        }
                        filterContext.Result = new RedirectToRouteResult(routeValues);
                        return;
                    }
                }
            }
            if (auth.IsValidKey(key) == false)
            {
                if (filterContext.HttpContext != null)
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Account", action = "LogOn", returnUrl = filterContext.HttpContext.Request.Url }));
            }
            // Adam's old code, possibly useful if we start logging requests on the DB in the future
            //else
            //{
            //    ////log the request
            //    //var log = new ActionRequestLog
            //    //{
            //    //    ActionName = filterContext.ActionDescriptor.ActionName,
            //    //    ControllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName,
            //    //    CreatorId = auth.GetActiveUser(key).Id,
            //    //    AccessDate = DateTime.UtcNow,
            //    //    SchoolId = 1, // need to get school Id
            //    //};
            //    //try
            //    //{
            //    //    log.IpAddress = filterContext.RequestContext.HttpContext.Request.ServerVariables["REMOTE_ADDR"];
            //    //}
            //    //catch (Exception)
            //    //{
            //    //    log.IpAddress = "Unknown";
            //    //}
            //    //var parameters = new StringBuilder();
            //    //foreach (var parameterKey in filterContext.ActionParameters.Keys)
            //    //{
            //    //    var parameterValue = filterContext.ActionParameters[parameterKey] ?? DomainConstants.ActionParameterNullValue;
            //    //    parameters.Append(string.Format("{0}={1}{2}", parameterKey, parameterValue, DomainConstants.ActionParameterDelimiter));
            //    //}
            //    //log.ActionParameters = parameters.ToString();

            //    ////save to azure table storage
            //    //DomainObjectHelpers.LogActionRequest(log);
            //}
        }
    }
}