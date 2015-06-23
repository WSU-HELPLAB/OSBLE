using System;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Controllers;
using OSBLEPlus.Logic.DomainObjects.Analytics;

namespace OSBLE.Areas.Analytics.Controllers
{
    [OsbleAuthorize]
    [IsAdmin]
    public class CalendarController : OSBLEController
    {
        //
        // GET: /Analytics/Calendar/
        public ActionResult Index()
        {
            return View("_Calendar", new CalendarAttributes { ReferenceDate = DateTime.Today });
            //return PartialView("_Calendar");
            //return PartialView("_Calendar", new CalendarAttributes {ReferenceDate = DateTime.Today});
        }

        public ActionResult GetCalendar()
        {
            return PartialView("_Calendar", new CalendarAttributes { ReferenceDate = DateTime.Today });
        }
    }
}
