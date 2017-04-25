using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Controllers;
using OSBLE.Models.Courses;

namespace OSBLE.Attributes
{
    /// <summary>
    /// Redirects to index if user is not an admin
    /// </summary>
    /// 
    public class IsInstructor : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!(filterContext.Controller is OSBLEController)) return;

            var controller = (OSBLEController) filterContext.Controller;
            
            if ((controller.CurrentUser == null) || !(controller.ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor ))
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" }));
            }
        }
    }
}