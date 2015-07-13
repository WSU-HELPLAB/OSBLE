using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Dapper;
using OSBLE.Attributes;
using OSBLE.Controllers;
using OSBLEPlus.Logic.DataAccess.Analytics;
using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Logic.Utility;

namespace OSBLE.Areas.Analytics.Controllers
{
    [OsbleAuthorize]
    [IsAdmin]
    public class TimelineController : OSBLEController
    {
        //
        // GET: /Analytics/Calendar/
        [ChildActionOnly]
        public ActionResult Index()
        {
                return PartialView("_Timeline", CourseDataAccess.GetStudentList(ActiveCourseUser.AbstractCourseID));       
        }

        public ActionResult GetCSVData(int scaleSetting, DateTime? timeFrom, DateTime? timeTo, int? timeout, bool? grayscale, bool? realtime, int courseId)
        {
            bool gray = grayscale ?? false;
            TimelineCriteria var = new TimelineCriteria {timeScale = (TimeScale) scaleSetting, timeFrom = timeFrom,
                timeTo = timeTo, timeout = timeout, grayscale = gray, courseId = courseId};

            var chartCsvData = TimelineVisualization.GetCSV(var, realtime);
            return File(new System.Text.UTF8Encoding().GetBytes(chartCsvData), "text/csv", "timeline.csv");
        }

        [ChildActionOnly]
        public ActionResult Options()
        {
            return PartialView("_TimelineOptions");
        }
    }
}
