using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Models.HomePage;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Data;
using OSBLE.Utility;


namespace OSBLE.Controllers
{
    public class TestController : Controller
    {
        //
        // GET: /Test/

        public ActionResult Index()
        {

            ViewBag.datetime = DateTime.Now.ToString();
            ViewBag.utcdateTime = DateTime.UtcNow.ToString();
            DateTime d = DateTime.Now;
            ViewBag.serverTimezone = TimeZone.CurrentTimeZone.StandardName.ToString();
            ViewBag.IsDaylightSavings = TimeZone.CurrentTimeZone.IsDaylightSavingTime(d).ToString();

            TimeZone curTimezone = TimeZone.CurrentTimeZone;
            TimeSpan currentOffset = curTimezone.GetUtcOffset(DateTime.Now);
            TimeSpan classTimeZone;

            ViewBag.CalculatedCurrentOffset = currentOffset.ToString();
            classTimeZone = new TimeSpan(-8, 0, 0);
            ViewBag.calculatedClassTimeZone = classTimeZone.ToString();
            TimeSpan difference = classTimeZone - currentOffset;
            ViewBag.vdifference = difference.ToString();

            DateTime StartTime = DateTime.Parse("1:00:00 PM");
            ViewBag.origStarttime = StartTime.ToString();

            DateTime EndTime = StartTime;

            EndTime = EndTime.Subtract(difference);
            ViewBag.StartTimeAfterOffset = EndTime.ToString();

            return View();
        }

    }
}
