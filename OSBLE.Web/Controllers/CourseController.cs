using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;

using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Models.HomePage;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Data;
using OSBLE.Utility;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
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

        private void createMeetingTimes(Course course, int utcOffset)
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
                    //DateTime beforeUtcStartTime = cm.StartTime;

                    //cm.StartTime = cm.StartTime.AddMinutes(utcOffset);
                    //cm.EndTime = cm.EndTime.AddMinutes(utcOffset);

                    //Check to see if the utc offset will change the day if so adjust the Meeting's date
                    //if (beforeUtcStartTime.DayOfYear != cm.StartTime.DayOfYear)
                    //{
                    //    int difference = (beforeUtcStartTime.DayOfYear - cm.StartTime.DayOfYear);
                    //    correctDay(cm, difference);
                    //}
                    course.CourseMeetings.Add(cm);
                }
            }

            db.SaveChanges();
        }

        //yc: not sure if this situation occurs anymore for below, currently not ever called
        //Correct the listed day because of utc offset
        private void correctDay(CourseMeeting cm, int difference)
        {
            //Days list
            //Create a dictionary to store the utc corrected day of the week
            Dictionary<string, int> DaysOfWeek = new Dictionary<string, int>();
            DaysOfWeek.Add("Sunday", 0);
            DaysOfWeek.Add("Monday", 0);
            DaysOfWeek.Add("Tuesday", 0);
            DaysOfWeek.Add("Wednesday", 0);
            DaysOfWeek.Add("Thursday", 0);
            DaysOfWeek.Add("Friday", 0);
            DaysOfWeek.Add("Saturday", 0);

            //Check to see if the difference needs to add or a take away a day
            if (difference > 0)
            {
                //Check each day to see what is marked un mark it and update the dictionary with the corrected utc day
                if (cm.Sunday == true)
                {
                    DaysOfWeek["Saturday"] = 1;
                    cm.Sunday = false;
                }
                if (cm.Monday == true)
                {
                    DaysOfWeek["Sunday"] = 1;
                    cm.Monday = false;
                }
                if (cm.Tuesday == true)
                {
                    DaysOfWeek["Monday"] = 1;
                    cm.Tuesday = false;
                }
                if (cm.Wednesday == true)
                {
                    DaysOfWeek["Tuesday"] = 1;
                    cm.Wednesday = false;
                }
                if (cm.Thursday == true)
                {
                    DaysOfWeek["Wednesday"] = 1;
                    cm.Thursday = false;
                }
                if (cm.Friday == true)
                {
                    DaysOfWeek["Thursday"] = 1;
                    cm.Friday = false;
                }
                if (cm.Saturday == true)
                {
                    DaysOfWeek["Friday"] = 1;
                    cm.Saturday = false;
                }
            }
            else
            {
                if (cm.Sunday == true)
                {
                    DaysOfWeek["Monday"] = 1;
                    cm.Sunday = false;
                }
                if (cm.Monday == true)
                {
                    DaysOfWeek["Tuesday"] = 1;
                    cm.Monday = false;
                }
                if (cm.Tuesday == true)
                {
                    DaysOfWeek["Wednesday"] = 1;
                    cm.Tuesday = false;
                }
                if (cm.Wednesday == true)
                {
                    DaysOfWeek["Thursday"] = 1;
                    cm.Wednesday = false;
                }
                if (cm.Thursday == true)
                {
                    DaysOfWeek["Friday"] = 1;
                    cm.Thursday = false;
                }
                if (cm.Friday == true)
                {
                    DaysOfWeek["Saturday"] = 1;
                    cm.Friday = false;
                }
                if (cm.Saturday == true)
                {
                    DaysOfWeek["Sunday"] = 1;
                    cm.Saturday = false;
                }
            }

            //Set the correct days after the utc offset is added
            foreach (KeyValuePair<string, int> day in DaysOfWeek)
            {
                if (day.Value == 1)
                {
                    if (day.Key == "Sunday")
                        cm.Sunday = true;
                    else if (day.Key == "Monday")
                        cm.Monday = true;
                    else if (day.Key == "Tuesday")
                        cm.Tuesday = true;
                    else if (day.Key == "Wednesday")
                        cm.Wednesday = true;
                    else if (day.Key == "Thursday")
                        cm.Thursday = true;
                    else if (day.Key == "Friday")
                        cm.Friday = true;
                    else if (day.Key == "Saturday")
                        cm.Saturday = true;
                }
            }

            return;
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

                int utcOffset = 0;
                try
                {
                    Int32.TryParse(Request.Form["utc-offset"].ToString(), out utcOffset);
                }
                catch (Exception)
                {
                }

                course.TimeZoneOffset = Convert.ToInt32(Request.Params["course_timezone"]);
                createMeetingTimes(course, utcOffset);
                createBreaks(course);

                // Make current user an instructor on new course.
                CourseUser cu = new CourseUser();
                cu.AbstractCourseID = course.ID;
                cu.UserProfileID = CurrentUser.ID;
                cu.AbstractRoleID = (int)CourseRole.CourseRoles.Instructor;


                db.CourseUsers.Add(cu);
                db.SaveChanges();

                Cache["ActiveCourse"] = course.ID;

                return RedirectToAction("Index", "Home");
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
            Course course = (Course)db.Courses.Find(ActiveCourseUser.AbstractCourseID);

            //Get the user's time zone cookie and convert it to int
            System.Web.HttpCookie cookieOffset = new System.Web.HttpCookie("utcOffset");
            cookieOffset = Request.Cookies["utcOffset"];
            int utcOffset;
            if (cookieOffset != null)
            {
                string UtcOffsetString = cookieOffset.Value;
                utcOffset = Convert.ToInt32(UtcOffsetString);
            }
            else
            {
                utcOffset = 0;
            }

            //If it exists, which it should update all of the meetings to reflect the correct utc adjusted time.
            if (utcOffset != 0)
            {
                ICollection<CourseMeeting> Meetings = course.CourseMeetings;
                foreach (CourseMeeting meeting in Meetings)
                {
                    DateTime beforeUtcStartTime = meeting.StartTime;

                    meeting.StartTime = meeting.StartTime.AddMinutes(-utcOffset);
                    meeting.EndTime = meeting.EndTime.AddMinutes(-utcOffset);

                    //Check to see if the utc offset will change the day if so adjust the Meeting's date
                    if (beforeUtcStartTime.DayOfYear != meeting.StartTime.DayOfYear)
                    {
                        int difference = (beforeUtcStartTime.DayOfYear - meeting.StartTime.DayOfYear);
                        correctDay(meeting, difference);
                    }
                }
            }
            else //Rare case where a cookie doesn't exist set the time to null essentially
            {
                ICollection<CourseMeeting> Meetings = course.CourseMeetings;
                foreach (CourseMeeting meeting in Meetings)
                {
                    meeting.StartTime = DateTime.Parse("00:00");
                    meeting.EndTime = DateTime.Parse("00:00");
                    meeting.Sunday = false;
                    meeting.Monday = false;
                    meeting.Tuesday = false;
                    meeting.Wednesday = false;
                    meeting.Thursday = false;
                    meeting.Friday = false;
                    meeting.Saturday = false;
                }
            }

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

            if (course.ID != ActiveCourseUser.AbstractCourseID)
            {
                return RedirectToAction("Home");
            }

            int utcOffset = 0;
            try
            {
                Int32.TryParse(Request.Form["utc-offset"].ToString(), out utcOffset);
            }
            catch (Exception)
            {
            }

            NameValueCollection parameters = Request.Params;

            Course updateCourse = (Course)ActiveCourseUser.AbstractCourse;

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

            createMeetingTimes(updateCourse, utcOffset);

            createBreaks(updateCourse);

            if (ModelState.IsValid)
            {
                db.Entry(updateCourse).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", "Home");
            }
            return View(course);
        }

        [HttpGet]
        [CanModifyCourse]
        public ActionResult Delete()
        {
            //our parent already supplies the active course information so we don't
            //have much to do here
            return View(ActiveCourseUser);
        }

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Delete(CourseUser cu)
        {
            //if the user clicked continue, then we should continue
            if (Request.Form.AllKeys.Contains("continue"))
            {
                ActiveCourseUser.AbstractCourse.IsDeleted = true;
                db.SaveChanges();
            }

            return RedirectToAction("Index", "Home"); 
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}