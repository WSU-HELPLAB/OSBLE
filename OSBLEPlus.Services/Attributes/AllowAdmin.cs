using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace OSBLEPlus.Services.Attributes
{
    public class AllowAdmin : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext filterContext)
        {
        }
    }
}