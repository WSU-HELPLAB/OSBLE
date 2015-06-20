using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Controllers;

namespace OSBLE.Areas.Analytics.Controllers
{
    [OsbleAuthorize]
    [IsAdmin]
    public class TimelineController : OSBLEController
    {
        //
        // GET: /Analytics/Calendar/
        public ActionResult Index()
        {
            //return View("Calendar", new CalendarAttributes { ReferenceDate = DateTime.Today });
            return PartialView("_Timeline");
        }
    }
}
