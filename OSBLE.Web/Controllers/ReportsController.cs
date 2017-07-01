using OSBLE.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Controllers;

namespace OSBLE.Controllers
{
    public class ReportsController : OSBLEController
    {
        public ActionResult PostReplyCounts()
        {
            return View();
        }

        [IsAdmin, HttpPost]
        public ActionResult PostReplyReport(string from, string to)
        {
            List<Tuple<string, int, int>> postsAndRepliesForCurrentCourse = DBHelper.GetPostsAndRepliesCount(ActiveCourseUser.AbstractCourseID, Convert.ToDateTime(from), Convert.ToDateTime(to));
            Tuple<DateTime, DateTime, List<Tuple<string, int, int>>> reportOutput = new Tuple<DateTime, DateTime, List<Tuple<string, int, int>>>(Convert.ToDateTime(from), Convert.ToDateTime(to), postsAndRepliesForCurrentCourse);
            ViewBag.ReportOutput = reportOutput;          
            return View();
        }
    }
}
