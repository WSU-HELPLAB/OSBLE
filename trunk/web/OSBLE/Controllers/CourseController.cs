using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{ 
    [Authorize]
    public class CourseController : OSBLEController
    {
        //
        // GET: /Course/

        public ActionResult Index()
        {
            return RedirectToAction("Edit");
        }

        [CanCreateCourses]
        public ActionResult Create()
        {
            return View(new Course());
        } 

        //
        // POST: /Course/Create

        [HttpPost]
        [CanCreateCourses]
        public ActionResult Create(Course course)
        {
            if (ModelState.IsValid)
            {
                db.Courses.Add(course);
                db.SaveChanges();

                // Make current user an instructor on new course.
                CoursesUsers cu = new CoursesUsers();
                cu.CourseID = course.ID;
                cu.UserProfileID = currentUser.ID;
                cu.CourseRoleID = (int)CourseRole.OSBLERoles.Instructor;

                db.CoursesUsers.Add(cu);
                db.SaveChanges();

                Session["ActiveCourse"] = course.ID;

                return RedirectToAction("Index");  
            }

            return View(course);
        }
        
        //
        // GET: /Course/Edit/5
 
        [RequireActiveCourse]
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult Edit()
        {
            ViewBag.CurrentTab = "Course Settings";
            Course course = (Course)db.Courses.Find(activeCourse.CourseID);
            return View(course);
        }

        //
        // POST: /Course/Edit/5

        [HttpPost]
        [RequireActiveCourse]
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult Edit(Course course)
        {
            ViewBag.CurrentTab = "Course Settings";

            if (course.ID != activeCourse.CourseID)
            {
                return RedirectToAction("Home");
            }

            Course updateCourse = (Course)activeCourse.Course;

            updateCourse.AllowDashboardPosts = course.AllowDashboardPosts;
            updateCourse.AllowDashboardReplies = course.AllowDashboardReplies;
            updateCourse.AllowEventPosting = course.AllowEventPosting;
            updateCourse.CourseCalendarWindowOfTime = course.CourseCalendarWindowOfTime;
            updateCourse.EndDate = course.EndDate;
            updateCourse.Name = course.Name;
            updateCourse.Number = course.Number;
            updateCourse.Prefix = course.Prefix;
            updateCourse.RequireInstructorApprovalForEventPosting = course.RequireInstructorApprovalForEventPosting;
            updateCourse.Semester = course.Semester;
            updateCourse.StartDate = course.StartDate;
            updateCourse.Year = course.Year;

            if (ModelState.IsValid)
            {
                db.Entry(updateCourse).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(course);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}