using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Controllers;


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
            ViewBag.CurrentTab = "Analytics";
            return View("Index");
            //return RedirectToAction("Index", "Calendar");
        }

        public ActionResult GetDefaultView()
        {
            return PartialView("~/Areas/Analytics/Views/Analytics/_Default.cshtml");
        }

        public ActionResult GetOptions()
        {
            return PartialView("~/Areas/Analytics/Views/Analytics/_DefaultOptions.cshtml");
        }
    }
}