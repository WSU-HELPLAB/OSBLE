using OSBLE.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Controllers
{
    public class HelpController : OSBLEController
    {
        //
        // GET: /Help/

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

        public ActionResult FileUploader()
        {
            return View();
        }
    }
}
