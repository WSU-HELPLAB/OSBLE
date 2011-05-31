using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models;
using OSBLE.Attributes;
using OSBLE.Models.HomePage;
using OSBLE.Models.Courses;

namespace OSBLE.Controllers
{ 
    [Authorize]
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

            if(ActiveCourse.Course is Course) {
                StartDate = (ActiveCourse.Course as Course).StartDate;
                EndDate = (ActiveCourse.Course as Course).EndDate;
            } else if(ActiveCourse.Course is Community) { 
                // For communities there are no start/end dates, so get earliest and latest events
                Event firstEvent = db.Events.Where(e => e.CourseID == ActiveCourse.CourseID).OrderBy(e => e.StartDate).FirstOrDefault();
                Event lastEvent = db.Events.Where(e => e.CourseID == ActiveCourse.CourseID).OrderByDescending(e => e.StartDate).FirstOrDefault();

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
            Event e = new Event();
            
            string start = null;

            // Start parameter present, use to populate start date.
            if (Request.Params["start"] != null)
            {
                start = Request.Params["start"];
                e.StartDate = DateTime.Parse(start);
                e.StartTime = e.StartDate;
            }

            return View(e);
        }

        //
        // POST: /Event/Create

        [HttpPost]
        [CanPostEvent]
        public ActionResult Create(Event e)
        {
            // Set to current user and poster
            e.Course = ActiveCourse.Course;
            e.Poster = currentUser;

            // Default to not Approved.
            e.Approved = false;

            // Combine start date and time fields into one field
            e.StartDate = e.StartDate.Date;
            e.StartDate = e.StartDate.AddHours(e.StartTime.Hour).AddMinutes(e.StartTime.Minute);

            // Approve if instructor/leader, course is community, or approval is not required.
            if (activeCourse.CourseRole.CanModify ||
                ((activeCourse.Course is Course) &&
                !(activeCourse.Course as Course).RequireInstructorApprovalForEventPosting) ||
                (activeCourse.Course is Community)
                )
            {
                e.Approved = true;
            }

            if (ModelState.IsValid)
            {
                db.Events.Add(e);
                db.SaveChanges();

                if (!e.Approved)
                {
                    NotificationController nc = new NotificationController();
                    nc.SendEventApprovalNotification(e);

                    return RedirectToAction("NeedsApproval");
                }
                return RedirectToAction("Index");  
            }

            return View(e);
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

            if (e.Course != ActiveCourse.Course)
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

            if (e.Course != ActiveCourse.Course)
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

            if (e.Course != ActiveCourse.Course)
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
            if (e.CourseID != ActiveCourse.CourseID)
            {
                return RedirectToAction("Index", "Home");
            }

            e.StartTime = e.StartDate;

            return View(e);
        }

        //
        // POST: /Event/Edit/5

        [CanModifyCourse]
        [HttpPost]
        public ActionResult Edit(Event e)
        {
            if (ModelState.IsValid)
            {
                // Validate original event. make sure it exists and is part of the active course.
                Event originalEvent = db.Events.Find(e.ID);
                if ((originalEvent == null) || (originalEvent.CourseID != ActiveCourse.CourseID))
                {
                    return RedirectToAction("Index", "Home");
                }

                // Update updateable fields
                originalEvent.Title = e.Title;

                originalEvent.StartDate = e.StartDate.Date;
                originalEvent.StartDate = originalEvent.StartDate.AddHours(e.StartTime.Hour).AddMinutes(e.StartTime.Minute);

                originalEvent.Link = e.Link;
                originalEvent.LinkTitle = e.LinkTitle;

                originalEvent.Description = e.Description;

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
                CoursesUsers cu = currentCourses.Where(c => c.Course == e.Course).FirstOrDefault();
                if (((cu != null) && (cu.CourseRole.CanModify)))
                {
                    db.Events.Remove(e);
                    db.SaveChanges();
                }
                else
                {
                    Response.StatusCode = 403;
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
            List<Event> events = ActiveCourse.Course.Events.Where(e => e.Approved && (e.StartDate >= StartDate) && (e.StartDate <= EndDate)).ToList();

            // Add course meeting times and breaks.
            if (ActiveCourse.Course is Course && ((ActiveCourse.Course as Course).ShowMeetings == true))
            {
                Course course = (Course)ActiveCourse.Course;

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
                            e.AllowLinking = true;
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

            return events.OrderBy(e => e.StartDate).ToList();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
