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
            OsbleAuthentication auth = new OsbleAuthentication();
            HttpCookie profileCookie = filterContext.HttpContext.Request.Cookies.Get(OsbleAuthentication.ProfileCookieKey);
            string aspName = filterContext.HttpContext.User.Identity.Name;
            
            //no cookie?  
            if (profileCookie == null)
            {
                //what about a asp.net auth cookie?
                if (aspName == null || aspName.Length == 0)
                {
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Account", action = "LogOn" }));
                }
                else
                {
                    //the user is logged in to asp.net, but doesn't have our custom auth cookie.  Create one now.
                    using(OSBLEContext db = new OSBLEContext())
                    {
                        UserProfile profile = db.UserProfiles.Where(u => u.AspNetUserName == aspName).FirstOrDefault();
                        profileCookie = auth.UserAsCookie(profile);
                        filterContext.HttpContext.Response.Cookies.Add(profileCookie);
                    }
                }
            }
            else
            {
                //get the user and then update the underlying asp.net authentication cookie
                UserProfile profile = auth.GetUserFromCookie(profileCookie);

                //method returns null when something wrong happened
                if (profile == null)
                {
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Account", action = "LogOn" }));
                }
                else
                {
                    HttpCookie updatedCookie = auth.UserAsCookie(profile);
                    filterContext.HttpContext.Response.Cookies.Add(profileCookie);
                    FormsAuthentication.SetAuthCookie(profile.AspNetUserName, true);
                }
            }
        }
    }
}