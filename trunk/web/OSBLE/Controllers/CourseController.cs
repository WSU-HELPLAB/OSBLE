using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses;

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

        private void createMeetingTimes(Course course)
        {
            int count = Convert.ToInt32(Request.Params["meetings_max"]);
            if (course.CourseMeetings == null)
            {
                course.CourseMeetings = new List<CourseMeeting>();
            }
            else
            {
                course.CourseMeetings.Clear();
            }

            for (int i = 0; i < count; i++)
            {
                if (Request.Params["meeting_location_" + i.ToString()] != null)
                {
                    CourseMeeting cm = new CourseMeeting();

                    cm.Name = Request.Params["meeting_name_" + i.ToString()];
                    cm.Location = Request.Params["meeting_location_" + i.ToString()];
                    cm.StartTime = DateTime.Parse(Request.Params["meeting_start_" + i.ToString()]);
                    cm.EndTime = DateTime.Parse(Request.Params["meeting_end_" + i.ToString()]);

                    cm.Sunday = Convert.ToBoolean(Request.Params["meeting_sunday_" + i.ToString()]);
                    cm.Monday = Convert.ToBoolean(Request.Params["meeting_monday_" + i.ToString()]);
                    cm.Tuesday = Convert.ToBoolean(Request.Params["meeting_tuesday_" + i.ToString()]);
                    cm.Wednesday = Convert.ToBoolean(Request.Params["meeting_wednesday_" + i.ToString()]);
                    cm.Thursday = Convert.ToBoolean(Request.Params["meeting_thursday_" + i.ToString()]);
                    cm.Friday = Convert.ToBoolean(Request.Params["meeting_friday_" + i.ToString()]);
                    cm.Saturday = Convert.ToBoolean(Request.Params["meeting_saturday_" + i.ToString()]);

                    course.CourseMeetings.Add(cm);
                }
            }
            db.SaveChanges();
        }

        private void createBreaks(Course course)
        {
            int count = Convert.ToInt32(Request.Params["breaks_max"]);
            if (course.CourseBreaks == null)
            {
                course.CourseBreaks = new List<CourseBreak>();
            }
            else
            {
                course.CourseBreaks.Clear();
            }

            for (int i = 0; i < count; i++)
            {
                if (Request.Params["break_name_" + i.ToString()] != null)
                {
                    CourseBreak cb = new CourseBreak();

                    cb.Name = Request.Params["break_name_" + i.ToString()];
                    cb.StartDate = DateTime.Parse(Request.Params["break_start_" + i.ToString()]);
                    cb.EndDate = DateTime.Parse(Request.Params["break_end_" + i.ToString()]);

                    course.CourseBreaks.Add(cb);
                }
            }
            db.SaveChanges();
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

                createMeetingTimes(course);
                createBreaks(course);

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

            NameValueCollection parameters = Request.Params;

            Course updateCourse = (Course)activeCourse.Course;

            updateCourse.Inactive = course.Inactive;
            updateCourse.AllowDashboardPosts = course.AllowDashboardPosts;
            updateCourse.AllowDashboardReplies = course.AllowDashboardReplies;
            updateCourse.AllowEventPosting = course.AllowEventPosting;
            updateCourse.CalendarWindowOfTime = course.CalendarWindowOfTime;
            updateCourse.EndDate = course.EndDate;
            updateCourse.Name = course.Name;
            updateCourse.Number = course.Number;
            updateCourse.Prefix = course.Prefix;
            updateCourse.RequireInstructorApprovalForEventPosting = course.RequireInstructorApprovalForEventPosting;
            updateCourse.Semester = course.Semester;
            updateCourse.StartDate = course.StartDate;
            updateCourse.Year = course.Year;
            updateCourse.ShowMeetings = course.ShowMeetings;

            // Default Late Policy
            updateCourse.MinutesLateWithNoPenalty = course.MinutesLateWithNoPenalty;
            updateCourse.HoursLatePerPercentPenalty = course.HoursLatePerPercentPenalty;
            updateCourse.HoursLateUntilZero = course.HoursLateUntilZero;
            updateCourse.PercentPenalty = course.PercentPenalty;

            createMeetingTimes(updateCourse);
            createBreaks(updateCourse);

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