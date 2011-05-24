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
            int currentCourseId = ViewBag.ActiveCourse.CourseID;

            //we need the current weights (tabs) for the current course
            var result = from weight in db.Weights
                    where weight.CourseID == currentCourseId
                    select weight;
            IEnumerable<Weight> tabs = result.ToList();

            //same thing for getting columns for the current tab (AbstractGradable)
            var assignmentResults = from gradable in db.Gradables
                     where gradable.WeightID == tabs.ElementAt(0).ID
                     select gradable;
            IEnumerable<Gradable> assignments = assignmentResults.ToList();

            //Getting the current grades
            var gradeResults = from gradable in db.Gradables
                               where gradable.WeightID == tabs.ElementAt(0).ID
                               select gradable;
            IEnumerable<AbstractGradable> grades = gradeResults.ToList();

            //save everything that we need to the viewebag
            ViewBag.Tabs = tabs;
            ViewBag.Assignments = assignments;
            ViewBag.Grades = grades;
            return View();
        }

    }
}
