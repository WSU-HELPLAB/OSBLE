using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Controllers;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentDetails.Controllers
{
    public class DetailsController : OSBLEController
    {
        //
        // GET: /AssignmentDetails/Details/

        public ActionResult Index(int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            return View(assignment);
        }

    }
}
