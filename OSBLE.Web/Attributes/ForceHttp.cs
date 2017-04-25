using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Attributes
{
    public class ForceHttp : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsSecureConnection == true)
            {
                filterContext.Result = new RedirectResult("http://" + filterContext.HttpContext.Request.ServerVariables["HTTP_HOST"] + filterContext.HttpContext.Request.RawUrl);
            }
        }
    }
}