using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Text;
using System.Web.Routing;

using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using OSBLE.Models.Assignments;
using OSBLE.Models;

using DDay;
using DDay.Collections;
using DDay.iCal;
using DDay.iCal.Serialization;
using DDay.iCal.Serialization.iCalendar;




namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    [RequireActiveCourse]
    public class iCalendarController : OSBLEController
    {
        //
        // GET: /iCalendar/
        
        public ViewResult Index()
        {
            ViewBag.CourseID = ActiveCourseUser.AbstractCourseID;
            return View();
        }

        public void CreateCourseCalendar(int id)
        {
            //Get the Course
            Course course = (from d in db.Courses
                             where d.ID == id
                             select d).FirstOrDefault();

            //get all the course events 
            List<OSBLE.Models.HomePage.Event> courseEvents = new List<Models.HomePage.Event>();

            using (var result = new EventController())
            {
                courseEvents = result.GetActiveCourseEvents(course.StartDate, course.EndDate);
            }

            //get the timezone of the course 
            CourseController cc = new CourseController();
            int utcOffset = (ActiveCourseUser.AbstractCourse as Course).TimeZoneOffset;
            TimeZoneInfo tz = cc.getTimeZone(utcOffset);

            //Create and initalize the Calendar object 
            iCalendar courseCalendar = new iCalendar();
            courseCalendar.AddTimeZone(tz);
            courseCalendar.Method = "PUBLISH";
            courseCalendar.Name = "VCALENDAR";
            courseCalendar.Version = "2.0";
            courseCalendar.ProductID = "-//Washington State University//OSBLE.org//EN";
            courseCalendar.Scale = "GREGORIAN";

            //add all the events to the calendar 
            foreach (OSBLE.Models.HomePage.Event e in courseEvents)
            {
                DDay.iCal.Event evt = courseCalendar.Create<DDay.iCal.Event>();

                evt.Start = new iCalDateTime(TimeZoneInfo.ConvertTimeFromUtc(e.StartDate, tz));
                if (e.EndDate == null)
                {
                    evt.End = evt.Start.AddDays(1);
                    evt.IsAllDay = true;
                }
                else
                {
                    evt.End = new iCalDateTime(TimeZoneInfo.ConvertTimeFromUtc(e.EndDate.Value, tz));
                }

                evt.Summary = e.Title;
                if (e.Description != null)
                    evt.Description = e.Description;
            }

            // Create a serialization context and serializer factory.
            // These will be used to build the serializer for our object.
            ISerializationContext ctx = new SerializationContext();
            ISerializerFactory factory = new DDay.iCal.Serialization.iCalendar.SerializerFactory();
            // Get a serializer for our object
            IStringSerializer serializer = factory.Build(courseCalendar.GetType(), ctx) as IStringSerializer;

            string output = serializer.SerializeToString(courseCalendar);
            var bytes = Encoding.UTF8.GetBytes(output);
            SaveCourseCalendar(bytes, course.ID);

        }
        public ActionResult DownloadCourseCalendar(int id)
        {
            Course course = (from d in db.Courses
                             where d.ID == id
                             select d).FirstOrDefault();

            //get all the course events 
            List<OSBLE.Models.HomePage.Event> courseEvents = new List<Models.HomePage.Event>();

            using (var result = new EventController())
            {
                courseEvents = result.GetActiveCourseEvents(course.StartDate, course.EndDate);
            }

            //get the timezone of the course 
            CourseController cc = new CourseController();
            int utcOffset = (ActiveCourseUser.AbstractCourse as Course).TimeZoneOffset;
            TimeZoneInfo tz = cc.getTimeZone(utcOffset);

            //Create and initalize the Calendar object 
            iCalendar courseCalendar = new iCalendar();
            courseCalendar.AddTimeZone(tz);
            courseCalendar.Method = "PUBLISH";
            courseCalendar.Name = "VCALENDAR";
            courseCalendar.Version = "2.0";
            courseCalendar.ProductID = "-//Washington State University//OSBLE.org//EN";
            courseCalendar.Scale = "GREGORIAN";

            //add all the events to the calendar 
            foreach (OSBLE.Models.HomePage.Event e in courseEvents)
            {
                DDay.iCal.Event evt = courseCalendar.Create<DDay.iCal.Event>();

                evt.Start = new iCalDateTime(TimeZoneInfo.ConvertTimeFromUtc(e.StartDate, tz));
                if (e.EndDate == null)
                {
                    evt.End = evt.Start.AddDays(1);
                    evt.IsAllDay = true;
                }
                else
                {
                    evt.End = new iCalDateTime(TimeZoneInfo.ConvertTimeFromUtc(e.EndDate.Value, tz));
                }

                evt.Summary = e.Title;
                if (e.Description != null)
                    evt.Description = e.Description;
            }

            // Create a serialization context and serializer factory.
            // These will be used to build the serializer for our object.
            ISerializationContext ctx = new SerializationContext();
            ISerializerFactory factory = new DDay.iCal.Serialization.iCalendar.SerializerFactory();
            // Get a serializer for our object
            IStringSerializer serializer = factory.Build(courseCalendar.GetType(), ctx) as IStringSerializer;

            string output = serializer.SerializeToString(courseCalendar);
            var contentType = "text/calendar";
            var bytes = Encoding.UTF8.GetBytes(output);

            return File(bytes, contentType, course.Prefix + course.Number + ".ics");

        }
        public ActionResult SubscribeToCalendar(int id)
        {
            //Get the Course
            Course course = (from d in db.Courses
                             where d.ID == id
                             select d).FirstOrDefault();

            string path = "http://osble.org/Content/iCal/" + course.ID + "/" + course.Prefix + course.Number + ".ics?nocache";

            if (System.IO.File.Exists(path))
            {
                return Redirect("http://www.google.com/calendar/render?cid=" + path);
            }

            return HttpNotFound();
        }
        public void SaveCourseCalendar(Byte[] courseCalendar, int id)
        {
            //Get the Course
            Course course = (from d in db.Courses
                             where d.ID == id
                             select d).FirstOrDefault();
            string path = "http://osble.org/Content/iCal/" + course.ID + "/";

#if DEBUG
            path = Server.MapPath("~/Content/iCal/" + course.ID + "/");
#endif

            //check if it exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            System.IO.File.WriteAllBytes(path + course.Prefix + course.Number + ".ics", courseCalendar);

        }
    }
}
