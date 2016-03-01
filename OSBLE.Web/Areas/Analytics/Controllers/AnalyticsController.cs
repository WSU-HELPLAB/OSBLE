using System;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Controllers;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLE.Utility;
using System.Collections.Generic;
using OSBLEPlus.Logic.DataAccess.Profiles;
using System.Linq;

namespace OSBLE.Areas.Analytics.Controllers
{
    [OsbleAuthorize]
    //[IsAdmin]
    [IsInstructor]
    
    public class AnalyticsController : OSBLEController
    {
        public ActionResult Index()
        {
            //for demo load calendar default
            //return RedirectToAction("Load", new { view = "Default" });
            return RedirectToAction("Load", new { view = "Calendar" });
        }

        public ActionResult Load(string view)
        {
            ViewBag.CurrentView = view;
            return View("Index");
        }

        [HttpPost]
        public JsonResult GetStudentsForCourseId(int courseId)
        {
            DateTime start = DBHelper.GetCourseStart(courseId);
            return Json(new {Students = CourseDataAccess.GetStudentList(courseId), StartYear = start.Year, StartMonth = start.Month });
        }
    }
}