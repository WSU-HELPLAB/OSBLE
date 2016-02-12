using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Controllers;

namespace OSBLE.Areas.Analytics.Controllers
{
    [OsbleAuthorize]
    [IsAdmin]
    public class DefaultController : OSBLEController
    {
        //
        // GET: /Analytics/Calendar/
        [ChildActionOnly]
        public ActionResult Index()
        {
            return PartialView("_Default");
        }

        [ChildActionOnly]
        public ActionResult Options()
        {
            return PartialView("_DefaultOptions");
        }
    }
}
