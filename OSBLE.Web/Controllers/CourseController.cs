using System;
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
        public void createMeetingTimes(Course course, int utcOffset)
        {
            TimeZoneInfo tz = getTimeZone(utcOffset);

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
                    //cm.StartTime = convertToUtc(utcOffset, cm.StartTime);
                    cm.StartTime = TimeZoneInfo.ConvertTimeToUtc(cm.StartTime, tz);
                    //cm.EndTime = convertToUtc(utcOffset, cm.EndTime);
                    cm.EndTime = TimeZoneInfo.ConvertTimeToUtc(cm.EndTime, tz);

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
            catch (TimeZoneNotFoundException e)
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
            catch (TimeZoneNotFoundException e)
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

        public void createBreaks(Course course)
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
                    cb.StartDate = convertToUtc(course.TimeZoneOffset, cb.StartDate);
                    cb.EndDate = convertToUtc(course.TimeZoneOffset, cb.EndDate);
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
                course.Prefix = course.Prefix.Replace(" ", "").ToUpper();
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
                using (iCalendarController icalControl = new iCalendarController())
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
                             select new
                             {
                                 d.IsDeleted,
                                 d.EndDate,
                                 d.Prefix
                             };

            //add them to a list as a selectlistitem
            List<SelectListItem> course = new List<SelectListItem>();
            List<string> allCoursePrefix = new List<string>();
            
            foreach (var c in CourseList)
                if(c.EndDate > DateTime.Now && !c.IsDeleted)
                     allCoursePrefix.Add(c.Prefix.ToUpper());

            //remove any duplicate course names
            allCoursePrefix = allCoursePrefix.Distinct().ToList();

            foreach(string prefix in allCoursePrefix)
                course.Add(new SelectListItem { Text = prefix, Value = prefix });

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
            if (number == "Search All")
            {
                var Results = from d in db.Courses
                              where d.Prefix == course 
                              select d;

                Results.GroupBy(x => x.Prefix)
                        .OrderBy(x => x.Count())
                        .Select(x => x.First());

                TempData["SearchResults"] = Results.Distinct().ToList();

                return RedirectToAction("CourseSearch", "Course");
            }
            else
            {
                var Results = from d in db.Courses
                              where d.Prefix.Contains(course) && d.Number == number
                              select d;

                Results.GroupBy(x => x.Number)
                        .OrderBy(x => x.Count())
                        .Select(x => x.First());

                TempData["SearchResults"] = Results.Distinct().ToList();

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
            updateCourse.Prefix = course.Prefix.Replace(" ", "").ToUpper();
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
                Clone.Prefix = pastCourse.Prefix.Replace(" ", "").ToUpper();
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
                
                Cache["ActiveCourse"] = clone.ID;

                return RedirectToAction("SelectAssignmentsToClone",new { courseID = oldCourse.ID });
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
        public bool CloneAllAssignmentsFromCourse(Course courseDestination, Course courseSource)
        {
            List<Assignment> previousAssignments = (from a in db.Assignments
                                                    where a.CourseID == courseSource.ID
                                                    select a).ToList();

            if (CopyAssignments(courseDestination, courseSource, previousAssignments))
                return true;
            else
                return false;

        }

        /// <summary>
        /// yc: with a given list of assignments, copy them from one course to another.
        /// </summary>
        /// <param name="courseDestination"></param>
        /// <param name="courseSource"></param>
        /// <param name="previousAssignments"></param>
        /// <returns></returns>
        public bool CopyAssignments(Course courseDestination, Course courseSource, List<Assignment> previousAssignments)
        {
            try
            {
                //calculate # of weeks since start date
                double difference = courseDestination.StartDate.Subtract(courseSource.StartDate).TotalDays;
                //for linking purposes, key == previous id, value == the clone course that is teh same
                Dictionary<int, int> linkHolder = new Dictionary<int, int>();
                foreach (Assignment p in previousAssignments)
                {
                    //disabling assignments that are not finished being handled yet
                    if (p.Type == AssignmentTypes.AnchoredDiscussion || p.Type == AssignmentTypes.CommitteeDiscussion
                            || p.Type == AssignmentTypes.ReviewOfStudentWork)
                        continue;

                    int prid = -1, paid = p.ID;
                    //for insert sake of cloned assigntment we must temprarly hold the list of assignments 
                    //whos id links to this assignment for temporary holding
                    List<Assignment> previouslyLinked = (from pl in db.Assignments
                                                         where pl.PrecededingAssignmentID == paid
                                                         select pl).ToList();
                    //remove the links for now
                    foreach (Assignment link in previouslyLinked)
                    {
                        link.PrecededingAssignmentID = null;
                        db.Entry(link).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    //tmp holders
                    if (p.RubricID != null)
                        prid = (int)p.RubricID;
                    //we are now ready for copying
                    Assignment na = new Assignment();
                    na = p;
                    na.CourseID = courseDestination.ID; //rewrite course id
                    na.IsDraft = true;
                    na.AssociatedEvent = null;
                    na.AssociatedEventID = null;
                    na.PrecededingAssignmentID = null;
                    na.AssignmentTeams = new List<AssignmentTeam>();
                    na.DiscussionTeams = new List<DiscussionTeam>();
                    na.ReviewTeams = new List<ReviewTeam>();
                    na.Deliverables = new List<Deliverable>();
    

                    //recalcualte new offsets for due dates on assignment
                    if (p.CriticalReviewPublishDate != null)
                    {
                        na.CriticalReviewPublishDate = ((DateTime)(p.CriticalReviewPublishDate)).Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    }
                    // to retain the time incase of in differt daylightsavings .. shifts
                    DateTime dd = convertFromUtc(courseSource.TimeZoneOffset, na.DueDate);
                    DateTime dt = convertFromUtc(courseSource.TimeZoneOffset, na.DueTime);
                    DateTime rd = convertFromUtc(courseSource.TimeZoneOffset, na.ReleaseDate);
                    DateTime rt = convertFromUtc(courseSource.TimeZoneOffset, na.ReleaseTime);
                    dd = dd.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    dt = dt.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    rd = rd.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    rt = rt.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    //convert back to utc
                    na.DueDate = convertToUtc(courseDestination.TimeZoneOffset, dd);
                    na.DueTime = convertToUtc(courseDestination.TimeZoneOffset, dt);
                    na.ReleaseDate = convertToUtc(courseDestination.TimeZoneOffset, rd);
                    na.ReleaseTime = convertToUtc(courseDestination.TimeZoneOffset, rt);
                    //we now have a base to save
                    db.Assignments.Add(na); 
                    db.SaveChanges();


                    //fix the link now
                    foreach (Assignment link in previouslyLinked)
                    {
                        link.PrecededingAssignmentID = paid;
                        db.Entry(link).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    linkHolder.Add(paid, na.ID); //for future assignment links

                    if (p.PrecededingAssignmentID != null)
                    {
                        na.PrecededingAssignmentID = linkHolder[(int)p.PrecededingAssignmentID];
                        na.PreceedingAssignment = db.Assignments.Find(linkHolder[(int)p.PrecededingAssignmentID]);
                        db.Entry(na).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    //copy assignmenttypes
                    if (p.Type == AssignmentTypes.DiscussionAssignment || p.Type == AssignmentTypes.CriticalReviewDiscussion)
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

                    //copy critical review settings
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
                            db.Entry(na).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }

                    //team eval
                    if (p.Type == AssignmentTypes.TeamEvaluation)
                    {
                        TeamEvaluationSettings ptes = (from tes in db.TeamEvaluationSettings
                                                       where tes.AssignmentID == paid
                                                       select tes).FirstOrDefault();

                        if (ptes != null)
                        {
                            TeamEvaluationSettings ntes = new TeamEvaluationSettings();
                            ntes.DiscrepancyCheckSize = ptes.DiscrepancyCheckSize;
                            ntes.RequiredCommentLength = ptes.RequiredCommentLength;
                            ntes.MaximumMultiplier = ptes.MaximumMultiplier;
                            ntes.AssignmentID = na.ID;
                            na.TeamEvaluationSettings = ntes;
                            db.Entry(na).State = EntityState.Modified;
                            db.SaveChanges();
                        }

                    }

                    //components
                    //rubrics
                    if (p.RubricID != null)
                        CopyRubric(p, na);

                    ///deliverables
                    List<Deliverable> pads = (from d in db.Deliverables
                                              where d.AssignmentID == paid
                                              select d).ToList();
                    foreach (Deliverable pad in pads)
                    {
                        Deliverable nad = new Deliverable();
                        nad.AssignmentID = na.ID;
                        nad.DeliverableType = pad.DeliverableType;
                        nad.Assignment = na;
                        nad.Name = pad.Name;
                        db.Deliverables.Add(nad);
                        db.SaveChanges();
                        na.Deliverables.Add(nad);
                        db.Entry(na).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    //abet stuff should prolly go here
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// yc: this copy the rubric information from one assignment to another. 
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Destination"></param>
        /// <returns>bool for if success or fail</returns>
        public bool CopyRubric(Assignment Source, Assignment Destination)
        {
            try
            {
                int sid = -1;

                if (Source.RubricID != null)
                {
                    sid = (int)Source.RubricID;
                    //create a new reburic thats an exact copy with the same critera
                    Rubric nr = new Rubric();
                    nr.HasGlobalComments = Source.Rubric.HasGlobalComments;
                    nr.HasCriteriaComments = Source.Rubric.HasCriteriaComments;
                    nr.EnableHalfStep = Source.Rubric.EnableHalfStep;
                    nr.EnableQuarterStep = Source.Rubric.EnableQuarterStep;
                    nr.Description = Source.Rubric.Description;
                    Destination.Rubric = nr;
                    db.Entry(Destination).State = EntityState.Modified;
                    db.SaveChanges();

                    //now get all the stuff for it
                    Dictionary<int, int> clevelHolder = new Dictionary<int, int>();
                    Dictionary<int, int> ccriterionHolder = new Dictionary<int, int>();

                    List<Level> pls = (from rl in db.Levels
                                       where rl.RubricID == sid
                                       select rl).ToList();
                    foreach (Level pl in pls)
                    {
                        Level nl = new Level();
                        nl.LevelTitle = pl.LevelTitle;
                        nl.PointSpread = pl.PointSpread;
                        nl.RubricID = nr.ID;
                        db.Levels.Add(nl);
                        db.SaveChanges();
                        clevelHolder.Add(pl.ID, nl.ID);
                    }

                    List<Criterion> prcs = (from rc in db.Criteria
                                            where rc.RubricID == sid
                                            select rc).ToList();

                    foreach (Criterion prc in prcs) //create a new criteron
                    {
                        Criterion nrc = new Criterion();
                        nrc.CriterionTitle = prc.CriterionTitle;
                        nrc.Weight = prc.Weight;
                        nrc.RubricID = nr.ID;
                        db.Criteria.Add(nrc);
                        db.SaveChanges();
                        ccriterionHolder.Add(prc.ID, nrc.ID);
                    }

                    //now descriptions
                    //for some reason, cell descriptions do not come with this assignment so lets do a search fo rit
                    List<CellDescription> pcds = (from cd in db.CellDescriptions
                                                  where cd.RubricID == sid
                                                  select cd).ToList();

                    foreach (CellDescription pcd in pcds)
                    {
                        CellDescription ncd = new CellDescription();
                        ncd.CriterionID = ccriterionHolder[pcd.CriterionID];
                        ncd.LevelID = clevelHolder[pcd.LevelID];
                        ncd.RubricID = nr.ID;
                        ncd.Description = pcd.Description;
                        db.CellDescriptions.Add(ncd);
                        db.SaveChanges();
                    }
                }
                if (Source.StudentRubricID != null)
                {
                    sid = (int)Source.StudentRubricID;
                    //create a new reburic thats an exact copy with the same critera
                    Rubric nr = new Rubric();
                    nr.HasGlobalComments = Source.Rubric.HasGlobalComments;
                    nr.HasCriteriaComments = Source.Rubric.HasCriteriaComments;
                    nr.Description = Source.Rubric.Description;
                    nr.EnableHalfStep = Source.Rubric.EnableHalfStep;
                    nr.EnableQuarterStep = Source.Rubric.EnableQuarterStep;
                    db.Rubrics.Add(nr);
                    db.SaveChanges();

                    Destination.StudentRubricID = nr.ID;
                    db.Entry(Destination).State = EntityState.Modified;
                    db.SaveChanges();

                    //now get all the stuff for it
                    Dictionary<int, int> slevelHolder = new Dictionary<int, int>();
                    Dictionary<int, int> scriterionHolder = new Dictionary<int, int>();

                    List<Level> pls = (from rl in db.Levels
                                       where rl.RubricID == sid
                                       select rl).ToList();
                    foreach (Level pl in pls)
                    {
                        Level nl = new Level();
                        nl.LevelTitle = pl.LevelTitle;
                        nl.PointSpread = pl.PointSpread;
                        nl.RubricID = nr.ID;
                        db.Levels.Add(nl);
                        db.SaveChanges();
                        slevelHolder.Add(pl.ID, nl.ID);
                    }

                    List<Criterion> prcs = (from rc in db.Criteria
                                            where rc.RubricID == sid
                                            select rc).ToList();

                    foreach (Criterion prc in prcs) //create a new criteron
                    {
                        Criterion nrc = new Criterion();
                        nrc.CriterionTitle = prc.CriterionTitle;
                        nrc.Weight = prc.Weight;
                        nrc.RubricID = nr.ID;
                        db.Criteria.Add(nrc);
                        db.SaveChanges();
                        scriterionHolder.Add(prc.ID, nrc.ID);
                    }

                    //now descriptions
                    //for some reason, cell descriptions do not come with this assignment so lets do a search fo rit
                    List<CellDescription> pcds = (from cd in db.CellDescriptions
                                                  where cd.RubricID == sid
                                                  select cd).ToList();

                    foreach (CellDescription pcd in pcds)
                    {
                        CellDescription ncd = new CellDescription();
                        ncd.CriterionID = scriterionHolder[pcd.CriterionID];
                        ncd.LevelID = slevelHolder[pcd.LevelID];
                        ncd.RubricID = nr.ID;
                        ncd.Description = pcd.Description;
                        db.CellDescriptions.Add(ncd);
                        db.SaveChanges();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// yc: get for view
        /// </summary>
        /// <param name="course"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult SelectAssignmentsToClone(int courseID)
        {
            ViewBag.cid = courseID;
            List<Assignment> previousAssignments = (from a in db.Assignments
                                                    where a.CourseID == courseID
                                                    select a).ToList();
            return View(previousAssignments);
        }

        /// <summary>
        /// yc: post for select assignment, passed the count beacuse we cannot have the funciton signature
        /// </summary>
        /// <param name="cID"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SelectAssignmentsToClone(int cID, int count)
        {
            Course o = (Course)(from c in db.AbstractCourses
                                where c.ID == cID
                                select c).FirstOrDefault();
            List<Assignment> previousAssignments = (from a in db.Assignments
                                                    where a.CourseID == cID
                                                    select a).ToList();
            List<int> i = new List<int>(); //the home of assignments we want to copy
            List<Assignment> n = new List<Assignment>();
            foreach (Assignment a in previousAssignments)
            {
                if (Request.Params["a_" + a.ID.ToString()] != null)
                {
                    i.Add(previousAssignments.IndexOf(a));
                }
            }
            foreach (int a in i)
            {
                n.Add(previousAssignments[a]);
            }
            if (n.Count > 0)
                CopyAssignments((ActiveCourseUser.AbstractCourse as Course), o, n);

            return RedirectToAction("Index", "Assignment");
        }

        public ActionResult DownloadDashboardPosts(int? id)
        {
            //get all the dashboard posts for this course 
            List<int> viewedCourses = new List<int>();
            List<DashboardPost> dashboardPosts = new List<DashboardPost>();
            DashboardPost dp = new DashboardPost();

            if (id == null)
            {
            viewedCourses.Add(ActiveCourseUser.AbstractCourseID);
                dashboardPosts = db.DashboardPosts.Where(d => viewedCourses.Contains(d.CourseUser.AbstractCourseID))
                                                                        .OrderBy(d => d.Posted).ToList();
            }
            else
            {
                dp = db.DashboardPosts.Find(id);
                dashboardPosts.Add(dp);
            }

            ///TODO: THIS WILL MOST LIKELY NEED TO CHANGE, don't want to mess with it right now
            //Should not be using httpcontext with new path method
            string path = HttpContext.Server.MapPath("~/App_Data/Cache/ActivityFeedExport/" + ActiveCourseUser.AbstractCourseID.ToString());

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            FileInfo info = new FileInfo(Path.Combine(path, "ActivityFeed.csv"));

            StreamWriter writer = info.CreateText();
            writer.WriteLine("PostNumber,Author,AuthorRole,Date,Content");
            int postCount = 1;
            
            foreach (var post in dashboardPosts)
            {
                string postContent = (post.Content.Replace("\"", "\"\"")).Replace("\r\n", " ");
                int replyCount = 1;
                writer.WriteLine(
                    postCount.ToString() + "," 
                    + post.CourseUser.UserProfile.Identification + "," 
                    + post.CourseUser.AbstractRole.Name + "," 
                    + (post.Posted.ToLocalTime()).ToString("MM/dd/yy H:mm:ss") + "," 
                    + "\"" + postContent + "\"");
                foreach (var reply in post.Replies)
                {
                    string replyContent = (reply.Content.Replace("\"", "\"\"")).Replace("\r\n", " ");
                    writer.WriteLine(
                        postCount.ToString() + "." + replyCount.ToString() + ","
                        + reply.CourseUser.UserProfile.Identification + "," 
                        + reply.CourseUser.AbstractRole.Name + ","
                        + (reply.Posted.ToLocalTime()).ToString("MM/dd/yy H:mm:ss") + "," 
                        + "\"" + replyContent + "\"");
                    replyCount++;
                }
                postCount++;
            }
            writer.Close();

            if ((ActiveCourseUser.AbstractCourse).GetType() != typeof (Course))
                return File(info.OpenRead(), "text/csv",
                    ActiveCourseUser.AbstractCourse.Name + "-Community-ActivityFeed.csv");

            var course = ActiveCourseUser.AbstractCourse as Course;
            return File(info.OpenRead(), "text/csv", ActiveCourseUser.AbstractCourse.Name + "-" + course.Semester + "-" + course.Year + "-ActivityFeed.csv");
        }
    }
}