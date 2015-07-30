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
        [ChildActionOnly]
        public ActionResult Index()
        {
            return PartialView("_Calendar", new CalendarAttributes { ReferenceDate = DateTime.Today });
        }
    }
}
