﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses.Rubrics;
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

        //yc: utcOffset is the zone offset information (eg PST == -8)
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

                    //yc ALL times must be in utc relative to the course it was created from
                    cm.StartTime = convertToUtc(utcOffset, cm.StartTime);
                    cm.EndTime = convertToUtc(utcOffset, cm.EndTime);

                    course.CourseMeetings.Add(cm);
                }
            }

            db.SaveChanges();
        }

        public TimeZoneInfo getTimeZone(int tzoffset)
        {
            string zone = "";
            switch (tzoffset)
            {
                case 0:
                    zone = "Greenwich Standard Time";
                    break;
                case 1:
                    zone = "W. Europe Standard Time";
                    break;
                case 2:
                    zone = "E. Europe Standard Time";
                    break;
                case 3:
                    zone = "Russian Standard Time";
                    break;
                case 4:
                    zone = "Arabian Standard Time";
                    break;
                case 5:
                    zone = "West Asia Standard Time";
                    break;
                case 6:
                    zone = "Central Asia Standard Time";
                    break;
                case 7:
                    zone = "North Asia Standard Time";
                    break;
                case 8:
                    zone = "Taipei Standard Time";
                    break;
                case 9:
                    zone = "Tokyo Standard Time";
                    break;
                case 10:
                    zone = "AUS Eastern Standard Time";
                    break;
                case 11:
                    zone = "Central Pacific Standard Time";
                    break;
                case 12:
                    zone = "New Zealand Standard Time";
                    break;
                case 13:
                    zone = "Tonga Standard Time";
                    break;
                case -1:
                    zone = "Cape Verde Standard Time";
                    break;
                case -2:
                    zone = "Mid-Atlantic Standard Time";
                    break;
                case -3:
                    zone = "E. South America Standard Time";
                    break;
                case -4:
                    zone = "Atlantic Standard Time";
                    break;
                case -5:
                    zone = "Eastern Standard Time";
                    break;
                case -6:
                    zone = "Central Standard Time";
                    break;
                case -7:
                    zone = "Mountain Standard Time";
                    break;
                case -8:
                    zone = "Pacific Standard Time";
                    break;
                case -9:
                    zone = "Alaskan Standard Time";
                    break;
                case -10:
                    zone = "Hawaiian Standard Time";
                    break;
                case -11:
                    zone = "Samoa Standard Time";
                    break;
                case -12:
                    zone = "Dateline Standard Time";
                    break;
                default:
                    zone = "";
                    break;
            }
            TimeZoneInfo tz;
            if (zone != "")
                tz = TimeZoneInfo.FindSystemTimeZoneById(zone);
            else
            {
                //going to assume utc
                tz = TimeZoneInfo.FindSystemTimeZoneById("Coordinate Universal Time");
            }
            return tz;

        }
        public DateTime convertToUtc(int tzoffset, DateTime originalTime)
        {
            DateTime convertedToUtc;
            TimeZoneInfo tz = getTimeZone(tzoffset);
            try
            {
                convertedToUtc = TimeZoneInfo.ConvertTimeToUtc(originalTime, tz);
            }
            catch(TimeZoneNotFoundException e)
            {
                //failed need to figure out what our error will be
                convertedToUtc = new DateTime();
            }
            
            return convertedToUtc;
        }

        public DateTime convertFromUtc(int tzOffset, DateTime originalTime)
        {
            DateTime convertedFromUtc;
            TimeZoneInfo tz = getTimeZone(tzOffset);
            try
            {
                convertedFromUtc = TimeZoneInfo.ConvertTimeFromUtc(originalTime, tz);
            }
            catch(TimeZoneNotFoundException e)
            {
                convertedFromUtc = new DateTime();
            }

            return convertedFromUtc;
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
                createMeetingTimes(course, course.TimeZoneOffset);
                createBreaks(course);

                // Make current user an instructor on new course.
                CourseUser cu = new CourseUser();
                cu.AbstractCourseID = course.ID;
                cu.UserProfileID = CurrentUser.ID;
                cu.AbstractRoleID = (int)CourseRole.CourseRoles.Instructor;


                db.CourseUsers.Add(cu);
                db.SaveChanges();

                Cache["ActiveCourse"] = course.ID;

                //will create a calendar for this course
                using(iCalendarController icalControl = new iCalendarController())
                {
                    icalControl.CreateCourseCalendar(course.ID);
                }

                return RedirectToAction("Index", "Home");
            }
            return View(course);
        }
        
        //Course Search
        [HttpGet]
        public ActionResult CourseSearch()
        {
            //get all instructors
            List<CourseUser> Instructors = db.CourseUsers.Where(cu => cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor).ToList();
            
            //get all the courses
            var CourseList = from d in db.Courses
                             where d.EndDate > DateTime.Now
                             select d;

            //add them to a list as a selectlistitem
            List<SelectListItem> course = new List<SelectListItem>();
            foreach(var c in CourseList)
            {   
                if(c.EndDate > DateTime.Now && !c.IsDeleted)
                    course.Add(new SelectListItem { Text = c.Prefix, Value = c.Prefix });
            }
            //remove any duplicate course names
            var finalList = course.GroupBy(x => x.Text).Select(x => x.OrderByDescending(y => y.Text).First()).ToList();

            //throw it in the view bag
            ViewBag.CourseName = new SelectList(finalList, "Value", "Text");
            ViewBag.SearchResults = TempData["SearchResults"];
            ViewBag.SearchResultsInstructors = Instructors;
            
            return View();
        }

        public JsonResult CourseNumber(string id)
        {
            var CourseNumber = from s in db.Courses
                               where s.Prefix == id
                               && s.IsDeleted == false
                               && s.EndDate > DateTime.Now
                               select s;            

            return Json(new SelectList(CourseNumber.ToArray(), "Number", "Number"), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SearchResults(string course, string number)
        {
            if(number == "Search All")
            {
                var Results = from d in db.Courses
                              where d.Prefix == course 
                              select d;

                Results.GroupBy(x => x.Prefix)
                        .OrderBy(x => x.Count())
                        .Select(x => x.First());

                TempData["SearchResults"] = Results.ToList();

                return RedirectToAction("CourseSearch", "Course");
            }
            else
            {
                var Results = from d in db.Courses
                              where d.Prefix == course && d.Number == number
                              select d;

                Results.GroupBy(x => x.Number)
                        .OrderBy(x => x.Count())
                        .Select(x => x.First());

                TempData["SearchResults"] = Results.ToList();

                return RedirectToAction("CourseSearch", "Course");
            }


        }

        public ActionResult ReqestCourseJoin(string id)
        {
            //get course from ID
            int intID = Convert.ToInt32(id);
            var request = (from c in db.Courses
                           where c.ID == intID
                           select c).FirstOrDefault();

            ViewBag.CourseName = request.Prefix.ToString() + " " + request.Number.ToString() + " - " + request.Name.ToString();

            //user is already enrolled in the course...dummy
            if (currentCourses.Select(x => x.AbstractCourse).Contains(request))
            {
                return View("CourseCurrentlyEnrolled");
            }
            
            //if the user is not enrolled in any courses but may be withdrawn in a course
            else if (ActiveCourseUser == null)
            {
                //this is for checking if they are withdrawn, poo will be null if they are not in course, else it will return a CourseUser
                var previousUser = (from d in db.CourseUsers
                           where d.UserProfileID == this.CurrentUser.ID
                           select d).FirstOrDefault();

                if (previousUser != null)
                {
                    CourseUser addedUser = new CourseUser();
                    UserProfile prf = previousUser.UserProfile;
                    
                    addedUser = previousUser;
                    addedUser.UserProfile = prf;
                    
                    addedUser.AbstractRoleID = (int)CourseRole.CourseRoles.Pending;
                    addedUser.AbstractCourseID = request.ID;
                    addedUser.Hidden = true;

                    db.CourseUsers.Add(addedUser);
                   // db.Entry(previousUser).State = EntityState.Modified;
                    db.SaveChanges();
                    ActiveCourseUser = previousUser;

                    using (NotificationController nc = new NotificationController())
                    {
                        nc.SendCourseApprovalNotification(request, previousUser);
                    }
                }
                else
                {

                    //we need to create a course user in order to send a proper notification to the instructor(s)
                    CourseUser newUser = new CourseUser();
                    UserProfile profile = db.UserProfiles.Where(up => up.ID == this.CurrentUser.ID).FirstOrDefault();
                    newUser.UserProfile = profile;
                    newUser.UserProfileID = profile.ID;
                    newUser.AbstractRoleID = (int)CourseRole.CourseRoles.Pending; //FIX THIS NOW FORREST
                    newUser.AbstractCourseID = request.ID;
                    newUser.Hidden = true;

                    db.CourseUsers.Add(newUser);
                    db.SaveChanges();

                    ActiveCourseUser = newUser;

                    using (NotificationController nc = new NotificationController())
                    {
                        nc.SendCourseApprovalNotification(request, newUser);
                    }
                }


                return View("CourseAwaitingApproval");
            }
            else
            {
                //send notification to instructors
                using (NotificationController nc = new NotificationController())
                {
                    //temporaly put them in the course as hidden
                    CourseUser tempUser = new CourseUser();
                    UserProfile profile = db.UserProfiles.Where(up => up.ID == this.CurrentUser.ID).FirstOrDefault();
                    tempUser.UserProfile = profile;
                    tempUser.UserProfileID = profile.ID;
                    tempUser.AbstractRoleID = (int)CourseRole.CourseRoles.Pending; //FIX THIS NOW FORREST
                    tempUser.AbstractCourseID = request.ID;
                    tempUser.Hidden = true;

                    db.CourseUsers.Add(tempUser);
                    db.SaveChanges();

                    nc.SendCourseApprovalNotification(request, tempUser);
                }
                return View("CourseAwaitingApproval");
            }
        }

        //Community Search 
        [HttpGet]
        public ActionResult CommunitySearch()
        {
            List<CourseUser> Leaders = db.CourseUsers.Where(cu => cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor).ToList();
            ViewBag.CommunitySearchResults = TempData["CommunitySearchResults"];
            ViewBag.SearchResultsLeaders = Leaders;
            return View();
        }

        [HttpPost]
        public ActionResult CommunitySearchResults(string name)
        {

            var Results = from d in db.Communities
                          where d.Name.Contains(name)
                          || d.Description.Contains(name)
                          || d.Name.Contains(name)
                          || d.Nickname.Contains(name)
                          select d;
            Results.GroupBy(x => x.Name)
                    .OrderBy(x => x.Count())
                    .Select(x => x.First());

            TempData["CommunitySearchResults"] = Results.ToList();

            return RedirectToAction("CommunitySearch", "Course");
        }

        public ActionResult ReqestCommunityJoin(string id)
        {
            int intID = Convert.ToInt32(id);
            var request = (from c in db.Communities
                           where c.ID == intID
                           select c).FirstOrDefault();

            ViewBag.CommunityName = request.Name.ToString();

            if (currentCourses.Select(x => x.AbstractCourse).Contains(request))
            {
                return View("CommunityCurrentlyEnrolled");
            }
            else if (ActiveCourseUser == null)
            {
                //we need to create a course user in order to send a proper notification to the instructor(s)
                CourseUser newUser = new CourseUser();
                UserProfile profile = db.UserProfiles.Where(up => up.ID == this.CurrentUser.ID).FirstOrDefault();
                newUser.UserProfile = profile;
                newUser.UserProfileID = profile.ID;
                newUser.AbstractRoleID = (int)CommunityRole.OSBLERoles.Pending; //FIX THIS NOW FORREST
                newUser.AbstractCourseID = request.ID;
                newUser.Hidden = true;

                db.CourseUsers.Add(newUser);
                db.SaveChanges();

                ActiveCourseUser = newUser;

                using (NotificationController nc = new NotificationController())
                {
                   //nc.SendCourseApprovalNotification(request, ActiveCourseUser);
                    nc.SendCommunityApprovalNotification(request, newUser);
                }

                return View("CommunityAwaitingApproval");
            }
            else
            {
                using (NotificationController nc = new NotificationController())
                {
                    //temporaly put them in the course as hidden
                    CourseUser tempUser = new CourseUser();
                    UserProfile profile = db.UserProfiles.Where(up => up.ID == this.CurrentUser.ID).FirstOrDefault();
                    tempUser.UserProfile = profile;
                    tempUser.UserProfileID = profile.ID;
                    tempUser.AbstractRoleID = (int)CommunityRole.OSBLERoles.Pending; //FIX THIS NOW FORREST
                    tempUser.AbstractCourseID = request.ID;
                    tempUser.Hidden = true;

                    db.CourseUsers.Add(tempUser);
                    db.SaveChanges();

                    nc.SendCommunityApprovalNotification(request, tempUser);
                }
                return View("CommunityAwaitingApproval");
            }
        }

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

            //yc: this edit no longer needs to check this anymore. may have to remove all of this if statement
            //If it exists, which it should update all of the meetings to reflect the correct utc adjusted time.
            //locate timezone offset
            int courseOffset = (ActiveCourseUser.AbstractCourse).GetType() != typeof(Community) ? ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset : 0;
            TimeZoneInfo tz = getTimeZone(courseOffset);

            if (utcOffset != 0)
            {
                ICollection<CourseMeeting> Meetings = course.CourseMeetings;
                foreach (CourseMeeting meeting in Meetings)
                {
                    meeting.StartTime = TimeZoneInfo.ConvertTimeFromUtc(meeting.StartTime, tz);
                    meeting.EndTime = TimeZoneInfo.ConvertTimeFromUtc(meeting.EndTime, tz);

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
            updateCourse.TimeZoneOffset = Convert.ToInt32(Request.Params["course_timezone"]);
            // Default Late Policy
            updateCourse.MinutesLateWithNoPenalty = course.MinutesLateWithNoPenalty;
            updateCourse.HoursLatePerPercentPenalty = course.HoursLatePerPercentPenalty;
            updateCourse.HoursLateUntilZero = course.HoursLateUntilZero;
            updateCourse.PercentPenalty = course.PercentPenalty;

            createMeetingTimes(updateCourse, updateCourse.TimeZoneOffset);

            createBreaks(updateCourse);

            if (ModelState.IsValid)
            {
                db.Entry(updateCourse).State = EntityState.Modified;
                db.SaveChanges();

                //will update a calendar for this course
                using (iCalendarController icalControl = new iCalendarController())
                {
                    icalControl.CreateCourseCalendar(course.ID);
                }

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
            //TODO: Delete course calendar 
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// yc: course cloning, for any course the current user has been an instructor in
        /// </summary>
        /// <returns></returns>
        [CanCreateCourses]
        public ActionResult CloneCourse()
        {
            //find all courses current users is in
            List<CourseUser> allUsersCourses = db.CourseUsers.Where(cu => cu.UserProfileID == CurrentUser.ID).ToList();
            List<CourseUser> previousInstructedCourses = allUsersCourses.Where(cu => (cu.AbstractCourse is Course)
                    &&
                    (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)).OrderByDescending(cu => (cu.AbstractCourse as Course).StartDate).ToList();
            ViewBag.pastCourses = previousInstructedCourses;
            return View();
        }

        [CanCreateCourses]
        [CanModifyCourse]
        public ActionResult CloneSetup(int courseid)
        {
            Course pastCourse = (from c in db.Courses
                                 where c.ID == courseid
                                 select c).FirstOrDefault();
            if (pastCourse != null)
            {
                Course Clone = new Course();
                Clone.AllowDashboardPosts = pastCourse.AllowDashboardPosts;
                Clone.AllowDashboardReplies = pastCourse.AllowDashboardReplies;
                Clone.AllowEventPosting = pastCourse.AllowEventPosting;
                Clone.CalendarWindowOfTime = pastCourse.CalendarWindowOfTime;
                Clone.HoursLatePerPercentPenalty = pastCourse.HoursLatePerPercentPenalty;
                Clone.HoursLateUntilZero = pastCourse.HoursLateUntilZero;
                Clone.MinutesLateWithNoPenalty = pastCourse.MinutesLateWithNoPenalty;             
                Clone.Name = pastCourse.Name;
                Clone.Number = pastCourse.Number;
                Clone.Prefix = pastCourse.Prefix;
                Clone.PercentPenalty = pastCourse.PercentPenalty;
                Clone.RequireInstructorApprovalForEventPosting = pastCourse.RequireInstructorApprovalForEventPosting;
                Clone.TimeZoneOffset = pastCourse.TimeZoneOffset;
                //clone upkeep stuff from original course
                Clone.ID = pastCourse.ID;
                return View(Clone);
            }
            else
            {
                //could not find it, send them an empty coures
                return RedirectToAction("Create");
            }
        }


        [HttpPost]
        [CanCreateCourses]
        [CanModifyCourse]
        public ActionResult CloneSetup(Course clone)
        {
            Course oldCourse = (Course)(from c in db.AbstractCourses
                                where c.ID == clone.ID
                                select c).FirstOrDefault();
            Course getNewId = new Course();
            clone.ID = getNewId.ID;
            

            if (ModelState.IsValid)
            {
                db.Courses.Add(clone);
                db.SaveChanges();

                int utcOffset = 0;
                try
                {
                    Int32.TryParse(Request.Form["utc-offset"].ToString(), out utcOffset);
                }
                catch (Exception)
                {
                }

                clone.TimeZoneOffset = Convert.ToInt32(Request.Params["course_timezone"]);
                createMeetingTimes(clone, clone.TimeZoneOffset);
                createBreaks(clone);

                // Make current user an instructor on new course.
                CourseUser cu = new CourseUser();
                cu.AbstractCourseID = clone.ID;
                cu.UserProfileID = CurrentUser.ID;
                cu.AbstractRoleID = (int)CourseRole.CourseRoles.Instructor;


                db.CourseUsers.Add(cu);
                db.SaveChanges();

                CloneAllAssignmentsFromCourse(clone, oldCourse);
                Cache["ActiveCourse"] = clone.ID;

                return RedirectToAction("Index", "Home");
            }
            return View(clone);
        }

        /// <summary>
        /// yc: all assignments should be cloned EXACTLY like the course source. find all the old assignments
        /// and create a new entry into the database with corresponding components also added
        /// </summary>
        /// <param name="courseDestination"></param>
        /// <param name="courseSource"></param>
        /// <returns></returns>
        public bool CloneAllAssignmentsFromCourse(Course courseDestination,Course courseSource)
        {
            List<Assignment> previousAssignments = (from a in db.Assignments
                                                    where a.CourseID == courseSource.ID
                                                    select a).ToList();

            //calculate # of weeks since start date

            double difference = courseDestination.StartDate.Subtract(courseSource.StartDate).TotalDays;
            //for linking purposes, key == previous id, value == the clone course that is teh same
            Dictionary<int, int> linkHolder = new Dictionary<int, int>();
            foreach (Assignment p in previousAssignments)
            {
                int prid = -1, paid = p.ID;
                //for insert sake of cloned assigntment
                //we must temprarly hold the list of assignments whos id links to this assignment for temporary holding
                List<Assignment> previouslyLinked = (from pl in db.Assignments
                                                     where pl.PrecededingAssignmentID == paid
                                                     select pl).ToList();
                foreach (Assignment link in previouslyLinked)
                {
                    link.PrecededingAssignmentID = null;
                    db.Entry(link).State = EntityState.Modified;
                    db.SaveChanges();
                }


                //copy details
                Assignment na = new Assignment();
                //tmp holders

                if (p.RubricID != null)
                    prid = (int)p.RubricID;
                na = p;
                na.CourseID = courseDestination.ID; //rewrite course id
                na.IsDraft = true;
                na.AssociatedEvent = null;
                na.AssociatedEventID = null;
                na.AssignmentTeams = new List<AssignmentTeam>();
                if (p.HasDeliverables)
                    na.Deliverables = new List<Deliverable>();
                if (p.HasDiscussionTeams) 
                    na.DiscussionTeams = new List<DiscussionTeam>();

                //recalcualte new offsets for due dates on assignment

                if (p.CriticalReviewPublishDate != null)
                {
                    na.CriticalReviewPublishDate = ((DateTime)(p.CriticalReviewPublishDate)).Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                }
                //else
                //    na.CriticalReviewPublishDate = null;
                DateTime dd = convertFromUtc(courseSource.TimeZoneOffset, na.DueDate);
                DateTime dt = convertFromUtc(courseSource.TimeZoneOffset, na.DueTime);
                DateTime rd = convertFromUtc(courseSource.TimeZoneOffset, na.ReleaseDate);
                DateTime rt = convertFromUtc(courseSource.TimeZoneOffset, na.ReleaseTime);

                dd = dd.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                dt = dt.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                rd = rd.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                rt = rt.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));

                na.DueDate = convertToUtc(courseDestination.TimeZoneOffset, dd);
                na.DueTime = convertToUtc(courseDestination.TimeZoneOffset, dt);
                na.ReleaseDate = convertToUtc(courseDestination.TimeZoneOffset, rd);
                na.ReleaseTime = convertToUtc(courseDestination.TimeZoneOffset, rt);

                

                db.Assignments.Add(na);
                db.SaveChanges();

                linkHolder.Add(paid, na.ID);

                //fix the link now
                foreach (Assignment link in previouslyLinked)
                {
                    link.PrecededingAssignmentID = paid;
                    db.Entry(link).State = EntityState.Modified;
                    db.SaveChanges();
                }

                if (p.Type == AssignmentTypes.DiscussionAssignment)
                {
                    DiscussionSetting pds = (from ds in db.DiscussionSettings
                                             where ds.AssignmentID == paid
                                             select ds).FirstOrDefault();

                    DiscussionSetting nds = new DiscussionSetting();
                    nds.InitialPostDueDate = pds.InitialPostDueDate.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    nds.InitialPostDueDueTime = pds.InitialPostDueDueTime.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    nds.AssociatedEventID = null;
                    nds.MaximumFirstPostLength = pds.MaximumFirstPostLength;
                    nds.MinimumFirstPostLength = pds.MinimumFirstPostLength;
                    nds.AnonymitySettings = pds.AnonymitySettings;
                    na.DiscussionSettings = nds;
                    db.Entry(na).State = EntityState.Modified;
                    db.SaveChanges();
                }
                //critical review
                if (p.Type == AssignmentTypes.CriticalReview)
                {
                    CriticalReviewSettings pcs = (from ds in db.CriticalReviewSettings
                                                  where ds.AssignmentID == paid
                                                  select ds).FirstOrDefault();

                    if (pcs != null)
                    {
                        CriticalReviewSettings ncs = new CriticalReviewSettings();
                        ncs.ReviewSettings = pcs.ReviewSettings;
                        na.CriticalReviewSettings = ncs;
                        na.PrecededingAssignmentID = linkHolder[(int)p.PrecededingAssignmentID];
                        db.Entry(na).State = EntityState.Modified;
                        db.SaveChanges();
                    }
    }
}