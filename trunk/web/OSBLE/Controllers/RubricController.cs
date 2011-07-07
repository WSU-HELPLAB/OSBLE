using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models;
using OSBLE.Models.ViewModels;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;
using OSBLE.Models.Users;

namespace OSBLE.Controllers
{ 
    public class RubricController : OSBLEController
    {
        [CanGradeCourse]
        public ActionResult Index()
        {
            Rubric rubric = db.Rubrics.Find(2);
            RubricViewModel viewModel = new RubricViewModel();
            viewModel.Rubric = rubric;

            //assignments are storied within categories, which are found within
            //the active course.  
            List<AbstractAssignment> assignments = new List<AbstractAssignment>();
            foreach (Category cat in (activeCourse.Course as Course).Categories)
            {
                foreach (AbstractAssignment assignment in cat.Assignments)
                {
                    assignments.Add(assignment);
                }
            }
            viewModel.Assignments = new SelectList(assignments, "ID", "Name");

            List<int> sections = (from cu in db.CoursesUsers
                                  where cu.CourseID == activeCourse.CourseID
                                  select cu.Section).Distinct().ToList();
            viewModel.Sections = new SelectList(sections);

            var users = (from cu in db.CoursesUsers
                                      where cu.CourseID == activeCourse.CourseID
                                      &&
                                      cu.Section == viewModel.SelectedSection
                                      &&
                                      cu.CourseRole.CanSubmit
                                      select new {ID = cu.UserProfile.ID, Name = cu.UserProfile.LastName + ", " + cu.UserProfile.FirstName}).ToList();
            viewModel.Students = new SelectList(users, "ID", "Name");

            return View(viewModel);
        }
    }
}