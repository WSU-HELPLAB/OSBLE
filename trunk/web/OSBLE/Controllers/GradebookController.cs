using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class GradebookController : OSBLEController
    {
        //
        // GET: /Gradebook/
        public GradebookController()
            : base()
        {
            ViewBag.CurrentTab = "Grades";
        }

        public ViewResult Index()
        {
            return View();
        }

    }
}
