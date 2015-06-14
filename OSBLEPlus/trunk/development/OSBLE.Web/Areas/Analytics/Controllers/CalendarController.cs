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
            return View("Calendar", new CalendarAttributes { ReferenceDate = DateTime.Today });
        }
    }
}
