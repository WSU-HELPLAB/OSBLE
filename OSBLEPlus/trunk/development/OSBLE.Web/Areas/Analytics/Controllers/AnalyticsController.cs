using System;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Controllers;
using OSBLEPlus.Logic.DomainObjects.Analytics;


namespace OSBLE.Areas.Analytics.Controllers
{
    [OsbleAuthorize]
    [IsAdmin]
    public class AnalyticsController : OSBLEController
    {
        //
        // GET: /Analytics/Calendar/
        public ActionResult Index()
        {
            return RedirectToAction("Load", new { view = "Default" });
        }

        public ActionResult Load(string view)
        {
            ViewBag.CurrentView = view;
            return View("Index");
        }
    }
}