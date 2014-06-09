using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using OSBLE.Models.Assignments;
using OSBLE.Models;


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
            DateTime StartTime = new DateTime();
            DateTime Endtime = new DateTime();
            //Account for utcOffset
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

            Event e = new Event();

            string start = null;

            // Start parameter present, use to populate start date.
            if (Request.Params["start"] != null)
            {
                start = Request.Params["start"];
                e.StartDate = DateTime.Parse(start);
                e.EndDate = e.StartDate;
                e.StartTime = (ActiveCourseUser.AbstractCourse as Course).CourseMeetings.FirstOrDefault().StartTime;
                e.EndTime = (ActiveCourseUser.AbstractCourse as Course).CourseMeetings.FirstOrDefault().EndTime;
                
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
                    e.StartTime = e.StartDate.AddHours(12.0) ;
                    e.EndTime = e.StartTime.AddHours(1.0);
                }
            }

            //e.EndTime = e.StartTime.Add(new TimeSpan(0, 1, 0, 0, 0));

            //e.StartTime = e.StartTime.AddMinutes(-utcOffset);
            //e.EndTime = e.EndTime.Value.AddMinutes(-utcOffset);

            return View(e);
        }

        //
        // POST: /Event/Create

        [HttpPost]
        [CanPostEvent]
        public ActionResult Create(Event e)
        {
            //Account for utcOffset
            int utcOffset = 0;
            try
            {
                Int32.TryParse(Request.Form["utc-offset"].ToString(), out utcOffset);
            }
            catch (Exception)
            {
            }

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
                //Account for utc time before saved
                //e.EndDate = e.EndDate.Value.AddMinutes(utcOffset);
                //e.StartDate = e.StartDate.AddMinutes(utcOffset);
                int courseOffset = ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset;
                e.EndDate = e.EndDate.Value.Subtract(new TimeSpan(courseOffset,0,0));
                e.StartDate = e.StartTime.Subtract(new TimeSpan(courseOffset, 0, 0));

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
                return RedirectToAction("Index");
            }

            return View(e);
        }

        /// <summary>
        /// This function will create an event that will start at the DueDate of the assignment, at Midnight. The event ends at the DueTime of the assignment.
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
            aEvent.Description = "[url:Assignment Page|osble.org/AssignmentDetails/" + assignment.ID + "]"; 
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
            dEvent.Description = "[url:Assignment Page|osble.org/AssignmentDetails/" + ds.AssignmentID + "]";
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
            e.StartTime = e.StartDate;
            if (e.EndDate != null)
            {
                e.EndTime = (DateTime)e.EndDate;
            }
            else
            {
                e.EndDate = e.StartDate;
                e.EndTime = e.StartTime;
            }

            //Check to see if there is a time zone cookie
            System.Web.HttpCookie cookieOffset = new System.Web.HttpCookie("utcOffset");
            cookieOffset = Request.Cookies["utcOffset"];
            int utcOffset;
            if (cookieOffset != null)
            {
                //Adjust the time if there is
                string UtcOffsetString = cookieOffset.Value;
                utcOffset = Convert.ToInt32(UtcOffsetString);
                int courseOffset = ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset;
                e.StartDate = e.StartDate.AddHours(courseOffset);
                e.EndDate = e.EndDate.Value.AddHours(courseOffset);
            }
            else
            {
                //If no cookie then the event's time is left to it's utc time
                utcOffset = 0;
            }
         
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

            int courseOffset = ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset;

            originalEvent.StartDate = e.StartDate.Subtract(new TimeSpan(courseOffset, 0, 0));// AddMinutes(utcOffset);
            originalEvent.StartTime = e.StartTime.Subtract(new TimeSpan(courseOffset, 0, 0));//.AddMinutes(utcOffset);

            if (!Request.Form.AllKeys.Contains("IncludeEndDate"))
            {
                originalEvent.EndDate = null;
            }
            else
            {
                originalEvent.EndDate = e.EndDate.Value.Subtract(new TimeSpan(courseOffset, 0, 0));//.AddMinutes(utcOffset);
                originalEvent.EndDate = e.EndTime.Value.Subtract(new TimeSpan(courseOffset, 0, 0));//.AddMinutes(utcOffset) ;
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
            List<Event> events = (from e in db.Events
                                   where e.Poster.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                                   && e.StartDate >= StartDate
                                   && e.StartDate <= EndDate
                                   && e.Approved
                                   select e).ToList();

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

                        if (cb.StartDate.Date != cb.EndDate.Date)
                        {
                            e.Title += " Starts";
                        }

                        e.StartDate = cb.StartDate.Date;
                        e.HideTime = true;
                        e.NoDateTime = true;
                        e.HideDelete = true;

                        events.Add(e);
                    }

                    // End of break (only if date is different than start)
                    if ((cb.StartDate.Date != cb.EndDate.Date) && (cb.EndDate >= StartDate) && (cb.EndDate <= EndDate))
                    {
                        Event e = new Event();

                        e.Title = cb.Name + " Ends";
                        e.StartDate = cb.EndDate.Date;
                        e.HideTime = true;
                        e.HideDelete = true;
                        events.Add(e);
                    }
                }

                int day = 0;

                // Loop through each date from StartDate to EndDate and generate Course Meetings
                while (true)
                {
                    DateTime current = StartDate.AddDays(day);
                    if (current > EndDate) break;

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

                            e.Title = cm.Name + " - " + cm.Location;
                            e.StartDate = current.AddHours((double)cm.StartTime.Hour).AddMinutes((double)cm.StartTime.Minute);
                            e.EndDate = current.AddHours((double)cm.EndTime.Hour).AddMinutes((double)cm.EndTime.Minute);
                            e.HideDelete = true;
                            //yc: compute offset
                            //
                            int courseOffset = ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset;
                            ViewBag.ctzoffset = courseOffset;
                            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                            DateTime pst = TimeZoneInfo.ConvertTime(DateTime.Now, zone);
                            if (pst.IsDaylightSavingTime())
                            {
                                //-8 becomes -7
                                e.StartDate = e.StartDate.AddHours(-1.0).AddMinutes(2.0);
                                e.StartTime = e.StartDate;
                                e.EndDate = e.EndDate.Value.AddHours(-1.0).AddMinutes(2.0);
                                e.EndTime = e.EndDate;
                            }
                            else
                            {
                                e.StartDate = e.StartDate.AddHours(-1.0).AddMinutes(5.0);
                                e.EndDate = e.EndDate.Value.AddHours(-1.0).AddMinutes(5.0);
                            }

                            //else its normal

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