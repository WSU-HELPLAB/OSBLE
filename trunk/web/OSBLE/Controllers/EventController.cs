using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;

namespace OSBLE.Controllers
{ 
    public class EventController : OSBLEController
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /Event/

        public ViewResult Index()
        {
            var events = db.Events.Include(e => e.Course).Include(e => e.Poster);
            return View(events.ToList());
        }

        //
        // GET: /Event/Details/5

        public ViewResult Details(int id)
        {
            Event e = db.Events.Find(id);
            return View(e);
        }

        //
        // GET: /Event/Create

        public ActionResult Create()
        {
            ViewBag.CourseID = new SelectList(db.AbstractCourses, "ID", "Name");
            ViewBag.PosterID = new SelectList(db.UserProfiles, "ID", "UserName");
            return View();
        } 

        //
        // POST: /Event/Create

        [HttpPost]
        public ActionResult Create(Event e)
        {
            if (ModelState.IsValid)
            {
                db.Events.Add(e);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            ViewBag.CourseID = new SelectList(db.AbstractCourses, "ID", "Name", e.CourseID);
            ViewBag.PosterID = new SelectList(db.UserProfiles, "ID", "UserName", e.PosterID);
            return View(e);
        }
        
        //
        // GET: /Event/Edit/5
 
        public ActionResult Edit(int id)
        {
            Event e = db.Events.Find(id);
            ViewBag.CourseID = new SelectList(db.AbstractCourses, "ID", "Name", e.CourseID);
            ViewBag.PosterID = new SelectList(db.UserProfiles, "ID", "UserName", e.PosterID);
            return View(e);
        }

        //
        // POST: /Event/Edit/5

        [HttpPost]
        public ActionResult Edit(Event e)
        {
            if (ModelState.IsValid)
            {
                db.Entry(e).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CourseID = new SelectList(db.AbstractCourses, "ID", "Name", e.CourseID);
            ViewBag.PosterID = new SelectList(db.UserProfiles, "ID", "UserName", e.PosterID);
            return View(e);
        }

        //
        // GET: /Event/Delete/5
 
        public ActionResult Delete(int id)
        {
            Event e = db.Events.Find(id);
            return View(e);
        }

        //
        // POST: /Event/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            Event e = db.Events.Find(id);
            db.Events.Remove(e);
            db.SaveChanges();
            return RedirectToAction("Index");
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