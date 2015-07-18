﻿using System.Web.Mvc;
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
            return RedirectToAction("Load", new { viewName = "Default" });
            //return RedirectToAction("Index", "Calendar");
        }

        public ActionResult Load(string viewName)
        {
            ViewBag.CurrentTab = "Analytics";
            ViewBag.CurrentView = viewName;
            return View("Index");
        }
    }
}