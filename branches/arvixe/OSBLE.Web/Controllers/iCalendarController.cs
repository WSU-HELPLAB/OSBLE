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
   
        /// <summary>
        /// Function is called to create the course ics file
        /// Function is called everytime the course calendar needs to be updated
        /// </summary>
        /// <param name="id"></param>
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
            courseCalendar.AddProperty("X-WR-CALNAME", course.Prefix + "-" + course.Number + "-" + course.Semester + "-" + course.Year);

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

        /// <summary>
        /// Function is called when the user request a course calendar ics file
        /// the file is generate upon request, therefore it will alwyas be up to date 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
            courseCalendar.AddProperty("X-WR-CALNAME", course.Prefix + "-" + course.Number + "-" + course.Semester + "-" + course.Year);

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

        /// <summary>
        /// Function is called when a users request to subscribe to a course calendar
        /// google is passed a publicly accessable link that points at the course ics file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult SubscribeToCalendar(int id)
        {
            //Get the Course
            Course course = (from d in db.Courses
                             where d.ID == id
                             select d).FirstOrDefault();
   
            string prefix = course.Prefix.Replace(@"/", "-");
            string number = course.Number.Replace(@"/", "-");

            string path = AppDomain.CurrentDomain.BaseDirectory + "Content/iCal/" + course.ID.ToString() + "/" + prefix + number + "-" + course.Semester + "-" + course.Year + ".ics";

            if (System.IO.File.Exists(path))
            {
                return Redirect("http://www.google.com/calendar/render?cid=" + path + "?nocache");
            }

            return HttpNotFound();
        }

        /// <summary>
        /// Function saves the course calendar to /Content/iCal/{CourseID}
        /// Called everytime the couse calendar must update
        /// </summary>
        /// <param name="courseCalendar"></param>
        /// <param name="id"></param>
        public void SaveCourseCalendar(Byte[] courseCalendar, int id)
        {
            //Get the Course
            Course course = (from d in db.Courses
                             where d.ID == id
                             select d).FirstOrDefault();

            string path = AppDomain.CurrentDomain.BaseDirectory + "Content/iCal/" + course.ID.ToString() + "/";

            //check if it exists
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string prefix = course.Prefix.Replace(@"/", "-");
            string number = course.Number.Replace(@"/", "-");

            System.IO.File.WriteAllBytes(path + prefix + number + "-" + course.Semester + "-" + course.Year + ".ics", courseCalendar);
      
    }
}
}
