﻿using OSBLE.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Courses;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    public class HelpController : OSBLEController
    {
        //
        // GET: /Help/
        public HelpController() : base()
        {
            if (null != ActiveCourseUser)
            {
                ViewBag.HideMail = OSBLE.Utility.DBHelper.GetAbstractCourseHideMailValue(ActiveCourseUser.AbstractCourseID);
            }
            else
            {
                ViewBag.HideMail = true;
            }
        }

        public ActionResult Index()
        {            
            return View();
        }

        public ActionResult CreateCourse()
        {
            return View();
        }

        public ActionResult AddingStudents()
        {
            return View();
        }

        public ActionResult AddingOthers()
        {
            return View();
        }

        public ActionResult CreateBasicAssignment()
        {
            return View();
        }

        public ActionResult CreateReviewAssignment()
        {
            return View();
        }

        public ActionResult CreateReviewDiscussion()
        {
            return View();
        }

        public ActionResult AddWebLinks()
        {
            return View();
        }

        public ActionResult Gradebook()
        {
            return View();
        }

        public ActionResult CreateRubric()
        {
            return View();
        }

        public ActionResult EditRubricOffline()
        {
            return View();
        }

        public ActionResult RosterGuide()
        {
            return View();
        }

        public ActionResult FileUploader()
        {
            return View();
        }

        public ActionResult UsingTeamBuilder()
        {
            return View();
        }
    }
}
