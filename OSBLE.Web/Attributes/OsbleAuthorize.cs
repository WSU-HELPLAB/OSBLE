using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
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
            var path = string.Format("{0}\\OSBLEPlus.Services\\App_Data\\", Directory.GetParent(a).FullName);

            var auth =
                new Authentication(path);

            if (OsbleAuthentication.CurrentUser == null && auth.GetActiveUser(filterContext.HttpContext.Request.QueryString["auth"]) == null)
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Account", action = "LogOn", returnUrl = filterContext.HttpContext.Request.Url }));
            }
        }
    }
}