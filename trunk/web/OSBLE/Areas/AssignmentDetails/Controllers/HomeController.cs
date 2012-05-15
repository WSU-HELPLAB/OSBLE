using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Controllers;

namespace OSBLE.Areas.AssignmentDetails.Controllers
{
    public class HomeController : OSBLEController
    {
        public ActionResult Index(int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            return View(assignment);
        }

    }
}
