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
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!(filterContext.Controller is OSBLEController)) return;

            var controller = (OSBLEController) filterContext.Controller;

            if ((controller.CurrentUser == null) || (!controller.CurrentUser.IsAdmin))
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" }));
            }
        }
    }
}