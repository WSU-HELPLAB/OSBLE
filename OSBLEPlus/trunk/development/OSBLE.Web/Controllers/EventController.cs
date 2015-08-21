using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.IO;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using OSBLE.Models.Assignments;
using OSBLE.Models;
using Dapper;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using OSBLEPlus.Logic;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using System.Configuration;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.Utility;
using OSBLE.Utility;


namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    [RequireActiveCourse]
    public class EventController : OSBLEController
    {
        //
        // GET: /Event/

        public ViewResult Index()
        {
            // Set range for all events
            DateTime StartDate = new DateTime();
            DateTime EndDate = new DateTime();
            //yc set time
 

            if (ActiveCourseUser.AbstractCourse is Course)
            {
                StartDate = (ActiveCourseUser.AbstractCourse as Course).StartDate;
                //AC: ticket #435 asks that events that go beyond the end of the class be displayed.
                //This is important for displaying final exam notices.
                EndDate = (ActiveCourseUser.AbstractCourse as Course).EndDate.Add(new TimeSpan(30, 0, 0, 0, 0));
 
            }
            else if (ActiveCourseUser.AbstractCourse is Community)
            {
                // For communities there are no start/end dates, so get earliest and latest events
                Event firstEvent = db.Events.Where(e => e.Poster.AbstractCourseID == ActiveCourseUser.AbstractCourseID).OrderBy(e => e.StartDate).FirstOrDefault();
                Event lastEvent = db.Events.Where(e => e.Poster.AbstractCourseID == ActiveCourseUser.AbstractCourseID).OrderByDescending(e => e.StartDate).FirstOrDefault();

                // If either event is null, return an empty list to the view.
                if ((firstEvent == null) || (lastEvent == null))
                {
                    return View(new List<Event>());
                }

                StartDate = firstEvent.StartDate;
                EndDate = lastEvent.StartDate;
            }

            List<Event> events = GetActiveCourseEvents(StartDate, EndDate).ToList();

            return View(events);
        }

        //
        // GET: /Event/Create
        [HttpGet]
        [CanPostEvent]
        public ActionResult Create()
        {
            //all times will display based off of course timezone.
            int utcOffset = ((Course) ActiveCourseUser.AbstractCourse).TimeZoneOffset;

            Event e = new Event();

            string start = null;

            // Start parameter present, use to populate start date.
            if (Request.Params["start"] != null)
            {
                start = Request.Params["start"];
                e.StartDate = DateTime.Parse(start);
                e.EndDate = e.StartDate;

                //these times should be in utc
                //start should contain the time in utc

                e.StartTime = e.StartDate;
                e.EndTime = e.StartTime.AddHours(1.0); //automatically add an hour
                
            }
            else
            {
                List<Event> nextday = GetActiveCourseEvents((ActiveCourseUser.AbstractCourse as Course).StartDate, (ActiveCourseUser.AbstractCourse as Course).EndDate);
                foreach (Event nd in nextday)
                {
                    if (nd.StartDate > DateTime.Now)
                    {
                        e.StartDate = nd.StartDate;
                        break;
                    }
                }
                e.EndDate = e.StartDate;
                //e.StartDate = (ActiveCourseUser.AbstractCourse as Course).CourseMeetings.FirstOrDefault().m
                //yc fixing for no course meetings crash
                if ((ActiveCourseUser.AbstractCourse as Course).CourseMeetings.Count > 0)
                {
                    e.StartTime = (ActiveCourseUser.AbstractCourse as Course).CourseMeetings.FirstOrDefault().StartTime;
                    e.EndTime = (ActiveCourseUser.AbstractCourse as Course).CourseMeetings.FirstOrDefault().EndTime;
                }
                else
                {
                    //default to noon for one hour
                    e.StartTime = e.StartDate.AddHours(12.0);
                    e.EndTime = e.StartTime.AddHours(1.0);
                    //not in utc time, just leave!
                    return View(e);
                }
            }
            //convert times to local course time for user viewing here 
            e.StartTime = e.StartTime.UTCToCourse(ActiveCourseUser.AbstractCourseID);
            e.EndTime = ((DateTime) (e.EndTime)).CourseToUTC(ActiveCourseUser.AbstractCourseID);

            return View(e);
        }

        //
        // POST: /Event/Create

        [HttpPost]
        [CanPostEvent]
        public ActionResult Create(Event e)
        {

            // Set to current user and poster
            e.Poster = ActiveCourseUser;

            // Default to not Approved.
            e.Approved = false;

            if (!Request.Form.AllKeys.Contains("IncludeEndDate"))
            {
                e.EndDate = null;
            }
            else
            {
                //make sure that the end date happens after the start
                if ((DateTime)e.EndDate < e.StartDate)
                {
                    ModelState.AddModelError("badDates", "The starting time must occur before the ending time");
                }
            }

            // Approve if instructor/leader, course is community, or approval is not required.
            if (ActiveCourseUser.AbstractRole.CanModify ||
                ((ActiveCourseUser.AbstractCourse is Course) &&
                !(ActiveCourseUser.AbstractCourse as Course).RequireInstructorApprovalForEventPosting) ||
                (ActiveCourseUser.AbstractCourse is Community)
                )
            {
                e.Approved = true;
            }

            if (ModelState.IsValid)
            {
                //locate timezone offset
                //int courseOffset = (ActiveCourseUser.AbstractCourse).GetType() != typeof(Community) ? ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset : 0;
                //CourseController cc = new CourseController();
                //TimeZoneInfo tz = cc.getTimeZone(courseOffset);

                //now convert the time to utc
                //if (e.EndDate != null)
                //    e.EndDate = TimeZoneInfo.ConvertTimeToUtc((DateTime)e.EndDate, tz);

                //e.StartDate = TimeZoneInfo.ConvertTimeToUtc(e.StartDate, tz);

                if (e.EndDate != null)
                    e.EndDate = ((DateTime) e.EndDate).CourseToUTC(ActiveCourseUser.AbstractCourseID);

                e.StartDate = e.StartDate.CourseToUTC(ActiveCourseUser.AbstractCourseID);

                db.Events.Add(e);
                db.SaveChanges();

                if (!e.Approved)
                {
                    using (NotificationController nc = new NotificationController())
                    {
                        nc.SendEventApprovalNotification(e);
                    }

                    return RedirectToAction("NeedsApproval");
                }
                //rebuilds course calendar file upon creating of a new event ICAL
                using (iCalendarController ical = new iCalendarController())
                {
                    ical.CreateCourseCalendar(ActiveCourseUser.AbstractCourseID);
                }

                return RedirectToAction("Index");

                

            }

            return View(e);
        }

        /// <summary>
        /// This function will create an event that will start at the 
        /// Date of the assignment, at Midnight. The event ends at the DueTime of the assignment.
        /// This event will be titled "AssignmentName Due"
        /// 
        /// In addition, if the assignment a discussion assignment, it will generate an additional event for "AssignmentName Initial Post Due". 
        /// It will also start at the InitialPostDueDate at midnight, and end at the InitialPostDueTime
        /// </summary>
        /// <param name="assignment">The assignment to make the event for</param>
        /// <param name="existingEvent">If an event already exists, it can be updated rather than adding a new one</param>
        [CanModifyCourse]
        static public void CreateAssignmentEvent(Assignment assignment, int ActiveCourseUserId, ContextBase db)
        {
            Event assignmentEvent = new Event();
            UpdateAssignmentEvent(assignment, assignmentEvent, ActiveCourseUserId, db);
            if (assignment.DiscussionSettings != null)
            {
                Event discussionEvent = new Event();
                UpdateDiscussionEvent(assignment.DiscussionSettings, discussionEvent, ActiveCourseUserId, db);
            }
        }

        /// <summary>
        /// This function will update an event with all assignment details.
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="dEvent"></param>
        [CanModifyCourse]
        static public void UpdateAssignmentEvent(Assignment assignment, Event aEvent, int ActiveCourseUserId, ContextBase db)
        {
            //Link to assignment details. Note, since this is hardcoded to osble.org, it will not work locally.
            aEvent.Description = "[url:Assignment Page|plus.osble.org/AssignmentDetails/" + assignment.ID + "]"; 
            aEvent.EndDate = null;
            aEvent.StartDate = assignment.DueDate;
            aEvent.StartTime = assignment.DueTime;
            aEvent.PosterID = ActiveCourseUserId;
            aEvent.Title = assignment.AssignmentName + " Due";
            aEvent.Approved = true;
            //aEvent.HideTime
            if (aEvent.ID == 0)
            {
                db.Events.Add(aEvent);
                db.SaveChanges();
                assignment.AssociatedEventID = aEvent.ID;
                db.Entry(assignment).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                db.Entry(aEvent).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
            if(assignment.CourseID.HasValue)
            {
                //rebuilds course calendar file upon assignment updates/creations
                using (iCalendarController ical = new iCalendarController())
                {
                    ical.CreateCourseCalendar(assignment.CourseID.Value);
                }
            }
            

        }

        /// <summary>
        /// This function will update an event with all discussion setting details.
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="dEvent"></param>
        [CanModifyCourse]
        static public void UpdateDiscussionEvent(DiscussionSetting ds, Event dEvent, int ActiveCourseUserId, ContextBase db)
        {
            //Link to assignment details. Note, since this is hardcoded to osble.org, it will not work locally.
            dEvent.Description = "[url:Assignment Page|plus.osble.org/AssignmentDetails/" + ds.AssignmentID + "]";
            dEvent.EndDate = null;
            dEvent.StartDate = ds.InitialPostDueDate;
            dEvent.StartTime = ds.InitialPostDueDueTime;
            dEvent.PosterID = ActiveCourseUserId;
            dEvent.Title = ds.Assignment.AssignmentName + " Initial Post(s) Due";
            dEvent.Approved = true;

            if (dEvent.ID == 0)
            {
                db.Events.Add(dEvent);
                db.SaveChanges();

            }
            ds.AssociatedEventID = dEvent.ID;
            db.Entry(ds).State = System.Data.EntityState.Modified;
            db.Entry(dEvent).State = System.Data.EntityState.Modified;
            db.SaveChanges();
        }

        public ViewResult NeedsApproval()
        {
            return View();
        }

        [CanModifyCourse]
        [HttpGet]
        public ActionResult Approval(int id)
        {
            Event e = db.Events.Find(id);

            if ((e == null) || (e.Approved))
            {
                return View("AlreadyApproved");
            }

            if (e.Poster.AbstractCourse != ActiveCourseUser.AbstractCourse)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(e);
        }

        [CanModifyCourse]
        [HttpPost]
        public ActionResult Approve(int id)
        {
            Event e = db.Events.Find(id);

            if (e.Poster.AbstractCourseID != ActiveCourseUser.AbstractCourseID)
            {
                return RedirectToAction("Index", "Home");
            }

            e.Approved = true;
            db.Entry(e).State = EntityState.Modified;
            db.SaveChanges();

            //rebuilds course calendar file upon event approval
            using (iCalendarController ical = new iCalendarController())
            {
                ical.CreateCourseCalendar(ActiveCourseUser.AbstractCourseID);
            }

            return RedirectToAction("Index", "Event");
        }

        [CanModifyCourse]
        [HttpPost]
        public ActionResult Reject(int id)
        {
            Event e = db.Events.Find(id);

            if (e.Poster.AbstractCourse != ActiveCourseUser.AbstractCourse)
            {
                return RedirectToAction("Index", "Home");
            }

            db.Events.Remove(e);
            db.SaveChanges();

            return RedirectToAction("Index", "Event");
        }

        //
        // GET: /Event/Edit/5

        [CanModifyCourse]
        [HttpGet]
        public ActionResult Edit(int id)
        {
            Event e = db.Events.Find(id);

            // Event not part of this course
            if (e.Poster.AbstractCourseID != ActiveCourseUser.AbstractCourseID)
            {
                return RedirectToAction("Index", "Home");
            }
            if (e.EndDate != null)
            {
                ViewBag.IncludeEndDate = "checked=\"checked\"";
            }
            else
            {
                ViewBag.IncludeEndDate = "";
            }

            //convert times from utc to show correctly for editing
  //          e.StartTime = e.StartTime.UTCToCourse(ActiveCourseUser.AbstractCourseID);
            e.StartDate = e.StartDate.UTCToCourse(ActiveCourseUser.AbstractCourseID);

            if (e.EndDate != null)
            {
                e.EndTime = ((DateTime)e.EndDate).UTCToCourse(ActiveCourseUser.AbstractCourseID);
            }
            else
            {
                e.EndDate = e.StartDate;
                e.EndTime = e.StartTime;
            }

            ////Check to see if there is a time zone cookie
            //System.Web.HttpCookie cookieOffset = new System.Web.HttpCookie("utcOffset");
            //cookieOffset = Request.Cookies["utcOffset"];
            //int utcOffset;
            //if (cookieOffset != null)
            //{
            //    //Adjust the time if there is
            //    string UtcOffsetString = cookieOffset.Value;
            //    utcOffset = Convert.ToInt32(UtcOffsetString);
            //    int courseOffset = ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset;
            //    e.StartDate = e.StartDate.AddHours(courseOffset);
            //    e.EndDate = e.EndDate.Value.AddHours(courseOffset);
            //}
            //else
            //{
            //    //If no cookie then the event's time is left to it's utc time
            //    utcOffset = 0;
            //}
         
            return View(e);
        }

        //
        // POST: /Event/Edit/5
        [CanModifyCourse]
        [HttpPost]
        public ActionResult Edit(Event e)
        {
            int utcOffset = 0;
            try
            {
                Int32.TryParse(Request.Form["utc-offset"].ToString(), out utcOffset);
            }
            catch (Exception)
            {
            }

            // Validate original event. make sure it exists and is part of the active course.
            Event originalEvent = db.Events.Find(e.ID);
            originalEvent.Title = e.Title;
            originalEvent.Description = e.Description;


            originalEvent.StartDate = e.StartDate.CourseToUTC(ActiveCourseUser.AbstractCourseID);
            originalEvent.StartTime = e.StartTime.CourseToUTC(ActiveCourseUser.AbstractCourseID);

            if (!Request.Form.AllKeys.Contains("IncludeEndDate"))
            {
                originalEvent.EndDate = null;
            }
            else
            {
                originalEvent.EndDate = ((DateTime)e.EndDate).CourseToUTC(ActiveCourseUser.AbstractCourseID);
                originalEvent.EndTime = ((DateTime)e.EndTime).CourseToUTC(ActiveCourseUser.AbstractCourseID);
                //make sure that the end date happens after the start
                if ((DateTime)originalEvent.EndDate < originalEvent.StartDate)
                {
                    ModelState.AddModelError("badDates", "The starting time must occur before the ending time");
                }             
            }
            if ((originalEvent == null) || (originalEvent.Poster.AbstractCourseID != ActiveCourseUser.AbstractCourseID))
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                db.Entry(originalEvent).State = EntityState.Modified;
                db.SaveChanges();

                //rebuilds course calendar file upon editing an event
                using (iCalendarController ical = new iCalendarController())
                {
                    ical.CreateCourseCalendar(ActiveCourseUser.AbstractCourseID);
                }

                return RedirectToAction("Index");
            }
            return View(e);
        }

        /// <summary>
        /// Removes an event
        /// </summary>
        /// <param name="id">ID of event to remove</param>
        /// <returns></returns>
        [HttpPost]
        [CanModifyCourse]
        public ActionResult Delete(int id)
        {
            Event e = db.Events.Find(id);

            if (e != null)
            {
                Assignment assignment = (from a in db.Assignments where a.AssociatedEventID == e.ID select a).FirstOrDefault();
                if (assignment != null)
                {
                    assignment.AssociatedEventID = null;
                    db.Events.Remove(e);
                    db.SaveChanges();
                }
                else
                {
                    db.Events.Remove(e);
                    db.SaveChanges();
                }

            }
            else
            {
                Response.StatusCode = 403;
            }
            return View("_AjaxEmpty");
        }

        [NonAction]
        public List<Event> GetActiveCourseEvents(DateTime StartDate, DateTime EndDate)
        {
            //MG:Pulling all events for this course that approved and have a startdate that 
            //lies within the StartDate and EndDate parameters. 
            List<Event> events = DBHelper.GetApprovedCourseEvents(ActiveCourseUser.AbstractCourseID, StartDate, EndDate).ToList<Event>();

            // NOTE to AJ: This is where I need to change the type from Event to List<FeedItems> for OSBLEPlus

            //List<FeedItems> events;

            /////////////////////////////////////
            /// TEST CODE FOR AJ USING DAPPER ///
            /////////////////////////////////////
            /*using (SqlConnection sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                                List<CourseUser> users = (from cu in db.CourseUsers
                                      where cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                                      && cu.AbstractRole.CanSubmit
                                      orderby cu.UserProfile.LastName, cu.UserProfile.FirstName
                                      select cu).ToList();
                
                List<CourseUser>testUsers = sqlConnection.Query<CourseUser>("SELECT * FROM CourseUsers " +
                                                                 "WHERE AbstractCourseID = @activeCourseId " +
                                                                 "AND AbstractRoleID = @roleId",
                    new {activeCourseId = ActiveCourseUser.AbstractCourseID, roleId = (int)CourseRole.CourseRoles.Student }).ToList();

                foreach (CourseUser cu in testUsers)
                {
                    cu.AbstractCourse = ActiveCourseUser.AbstractCourse;

                    cu.UserProfile = sqlConnection.Query<UserProfile>("SELECT * FROM UserProfiles " +
                                                     "WHERE UserProfileID = ID").FirstOrDefault();

                    

                }
            }*/
            
            //yc: daylight savings thigns
            //int courseOffset = ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset;
            int courseOffset = (ActiveCourseUser.AbstractCourse).GetType() != typeof(Community) ? ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset : 0;
            ViewBag.ctzoffset = courseOffset;

            // Add course meeting times and breaks.
            if (ActiveCourseUser.AbstractCourse is Course && ((ActiveCourseUser.AbstractCourse as Course).ShowMeetings == true))
            {
                Course course = (Course)ActiveCourseUser.AbstractCourse;
                
                // Add breaks within window to events
                foreach (CourseBreak cb in course.CourseBreaks)
                {
                    // Start of break
                    if ((cb.StartDate >= StartDate) && (cb.StartDate <= EndDate))
                    {
                        Event e = new Event();

                        e.Title = cb.Name;

                        if (cb.StartDate != cb.EndDate)
                        {
                            e.Title += " Starts";
                        }

                        //e.StartDate = cb.StartDate.Date;
                        e.StartDate = cb.StartDate;
                        e.HideTime = true;
                        e.NoDateTime = true;
                        e.HideDelete = true;

                        events.Add(e);
                    }

                    var temp1 = cb.StartDate;
                    var temp2 = cb.StartDate.Date;

                    // End of break (only if date is different than start)
                    if ((cb.StartDate != cb.EndDate) && (cb.EndDate >= StartDate) && (cb.EndDate <= EndDate))
                    {
                        Event e = new Event();

                        e.Title = cb.Name + " Ends";
                        //e.StartDate = cb.EndDate.Date;
                        e.StartDate = cb.EndDate;
                        e.HideTime = true;
                        e.NoDateTime = true;
                        e.HideDelete = true;
                        events.Add(e);
                    }
                }

                int day = 0;

                // Loop through each date from StartDate to EndDate and generate Course Meetings
                while (true)
                {
                    DateTime current = StartDate.AddDays(day);
                    if (current > EndDate) 
                        break;

                    foreach (CourseMeeting cm in course.CourseMeetings)
                    {
                        // Wow, this is a big if statement.
                        if (
                            ((current.DayOfWeek == DayOfWeek.Sunday) && cm.Sunday) ||
                            ((current.DayOfWeek == DayOfWeek.Monday) && cm.Monday) ||
                            ((current.DayOfWeek == DayOfWeek.Tuesday) && cm.Tuesday) ||
                            ((current.DayOfWeek == DayOfWeek.Wednesday) && cm.Wednesday) ||
                            ((current.DayOfWeek == DayOfWeek.Thursday) && cm.Thursday) ||
                            ((current.DayOfWeek == DayOfWeek.Friday) && cm.Friday) ||
                            ((current.DayOfWeek == DayOfWeek.Saturday) && cm.Saturday)
                            )
                        {
                            Event e = new Event();

                            //printing in utc time for browser to convert for you
                            CourseController cc = new CourseController();
                            e.Title = cm.Name + " - " + cm.Location;
                            //to combat timezone shifts 
                            e.StartDate = current.AddHours((double)cm.StartTime.Hour).AddMinutes((double)cm.StartTime.Minute);
                            e.EndDate = current.AddHours((double)cm.EndTime.Hour).AddMinutes((double)cm.EndTime.Minute);
                            e.HideDelete = true;

                            // Do not show Course meetings outside of course start/end date and breaks.
                            if ((e.StartDate.Date >= course.StartDate.Date) && (e.StartDate.Date <= course.EndDate.Date) && (course.CourseBreaks.Where(b => (current >= b.StartDate) && (current <= b.EndDate)).Count() < 1))
                            {
                                events.Add(e);
                            }
                        }
                    }
                    day++; // Add one day
                }
            }

            return events.OrderBy(e => e.ID).OrderBy(e => e.StartDate).ToList();
        }
    
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}