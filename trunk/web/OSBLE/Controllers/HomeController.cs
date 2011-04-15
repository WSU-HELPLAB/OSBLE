using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Controllers
{
    public class HomeController : OSBLEController
    {
        [Authorize]
        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";

            return View();
        }

        [Authorize]
        public ActionResult About()
        {
            return View();
        }
    }
}
