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

        public ActionResult Index()
        {
            int currentCourseId = ActiveCourse.CourseID;

            //we need the current weights (tabs) for the current course
            var result = from weight in db.Weights
                    where weight.CourseID == currentCourseId
                    select weight;
          
            //If no tabs exist, we need to create an untitled tab
            if (result.Count() == 0)
            {
                Weight newWeight = new Weight()
                {
                    Name = "Untitled",
                    CourseID = currentCourseId,
                    Course = ViewBag.ActiveCourse.Course,
                    Points = 0,
                    Gradables = new List<AbstractGradable>()
                };

                db.Weights.Add(newWeight);
                db.SaveChanges();

                result = from weight in db.Weights
                         where weight.CourseID == currentCourseId
                         select weight;
            }

            List<Weight> tabs = result.ToList();
            int gradableId = tabs.ElementAt(0).ID;

            //get course users
            var userResults = from users in db.CoursesUsers
                              where users.CourseID == currentCourseId 
                              && users.CourseRoleID == (int)CourseRole.OSBLERoles.Student
                              select users;
            List<CoursesUsers> user = userResults.ToList();

            //same thing for getting columns for the current tab (AbstractGradable)
            var assignmentResults = from gradable in db.AbstractGradables
                                    where gradable.WeightID == gradableId
                                    select gradable;

            List<AbstractGradable> assignments = assignmentResults.ToList();

            //Getting the current grades
            var gradeResults = from gradable in db.GradableScores
                               where gradable.Gradable.WeightID == gradableId 
                               select gradable;
            List<GradableScore> grades = gradeResults.ToList();

            //save everything that we need to the viewebag
            ViewBag.Tabs = tabs;
            ViewBag.Assignments = assignments;
            ViewBag.Grades = grades;
            ViewBag.Users = user;
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

    }
}
