using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
using System.Threading.Tasks;




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
            var course = (from d in db.Courses
                          where d.ID == id
                          select new
                          {
                              d.StartDate,
                              d.EndDate,
                              d.Prefix,
                              d.Number,
                              d.Semester,
                              d.Year,
                              d.CourseMeetings,
                              d.CourseBreaks,
                              d.ID
                          }).FirstOrDefault();


            //get course events
            var events = (from e in db.Events
                          where e.Poster.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                          && e.StartDate >= course.StartDate
                          && e.StartDate <= course.EndDate
                          && e.Approved
                          select new
                          {
                              e.StartDate,
                              e.EndDate,
                              e.Title,
                              e.Description
                          }).ToList();

            //Create the calendar object 
            iCalendar courseCalendar = new iCalendar();
            //initalize the Calendar object 
            courseCalendar.AddTimeZone(DateTimeExtensions.GetTimeZone(((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset));
            courseCalendar.Method = "PUBLISH";
            courseCalendar.Name = "VCALENDAR";
            courseCalendar.Version = "2.0";
            courseCalendar.ProductID = "-//Washington State University//OSBLE.org//EN";
            courseCalendar.Scale = "GREGORIAN";
            courseCalendar.AddProperty("X-WR-CALNAME", course.Prefix + "-" + course.Number + "-" + course.Semester + "-" + course.Year);

            //get course breaks
            if (ActiveCourseUser.AbstractCourse is Course && ((ActiveCourseUser.AbstractCourse as Course).ShowMeetings == true))
            {
                foreach (CourseMeeting cm in course.CourseMeetings)
                {
                    StringBuilder rpPattern = new StringBuilder("FREQ=WEEKLY;UNTIL=");
                    rpPattern.Append(new iCalDateTime(course.EndDate.UTCToCourse(ActiveCourseUser.AbstractCourseID)).ToString(@"yyyyMMdd\THHmmss\Z"));
                    rpPattern.Append(";WKST=SU;BYDAY=");
                    if (cm.Sunday)
                        rpPattern.Append("SU,");
                    if (cm.Monday)
                        rpPattern.Append("MO,");
                    if (cm.Tuesday)
                        rpPattern.Append("TU,");
                    if (cm.Wednesday)
                        rpPattern.Append("WE,");
                    if (cm.Thursday)
                        rpPattern.Append("TH,");
                    if (cm.Friday)
                        rpPattern.Append("FR,");
                    if (cm.Saturday)
                        rpPattern.Append("SA");

                    //trim trailing comma if it is there
                    if (rpPattern[rpPattern.Length - 1] == ',')
                        rpPattern.Remove(rpPattern.Length - 1, 1);

                    RecurringComponent recurringComponent = new RecurringComponent();
                    RecurrencePattern pattern = new RecurrencePattern(rpPattern.ToString());

                    DDay.iCal.Event evt = courseCalendar.Create<DDay.iCal.Event>();
                    //may cause issues
                    DateTime evtStart = DateTime.SpecifyKind(course.StartDate, DateTimeKind.Utc);
                    evtStart = evtStart.Add(cm.StartTime.TimeOfDay);
                    DateTime evtEnd = DateTime.SpecifyKind(course.StartDate, DateTimeKind.Utc);
                    evtEnd = evtEnd.Add(cm.EndTime.TimeOfDay);

                    //we need to test if right NOW is in DST.
                    //case: course starts during DST, and it's now after DST, result: after DST event is now wrong.
                    DateTime currentTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                                                        evtStart.Hour, evtStart.Minute, evtStart.Second, DateTimeKind.Utc);
                    bool isDaylight = TimeZoneInfo.Local.IsDaylightSavingTime(currentTime.UTCToCourse(ActiveCourseUser.AbstractCourseID));

                    if (isDaylight)
                    {
                        evt.Start = new iCalDateTime(evtStart.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                        evt.End = new iCalDateTime(evtEnd.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                        evt.LastModified = new iCalDateTime(DateTime.Now);
                        evt.Summary = cm.Name;
                        evt.Location = cm.Location;
                        evt.RecurrenceRules.Add(pattern);
                    }
                    else //no longer DST
                    {
                        bool matchingStartDay = false;
                        while (!matchingStartDay)
                        {
                            switch (currentTime.DayOfWeek)
                            {
                                case DayOfWeek.Monday:
                                    if (cm.Monday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Tuesday:
                                    if (cm.Tuesday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Wednesday:
                                    if (cm.Wednesday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Thursday:
                                    if (cm.Thursday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Friday:
                                    if (cm.Friday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Saturday:
                                    if (cm.Saturday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Sunday:
                                    if (cm.Sunday)
                                        matchingStartDay = true;
                                    break;
                                default:
                                    matchingStartDay = false;
                                    break;
                            }

                            if (!matchingStartDay)
                            {
                                currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.AddDays(1).Day,
                                                        evtStart.Hour, evtStart.Minute, evtStart.Second, DateTimeKind.Utc);
                            }
                        }

                        evt.Start = new iCalDateTime(currentTime.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                        evt.End = new iCalDateTime(evtEnd.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                        evt.LastModified = new iCalDateTime(DateTime.Now);
                        evt.Summary = cm.Name;
                        evt.Location = cm.Location;
                        evt.RecurrenceRules.Add(pattern);
                    }

                }

                //create the course breaks 
                foreach (CourseBreak cb in course.CourseBreaks)
                {
                    DDay.iCal.Event evt = courseCalendar.Create<DDay.iCal.Event>();
                    DateTime evtStart = cb.StartDate.Date;
                    DateTime evtEnd = cb.EndDate.Date.AddDays(1);

                    evt.Summary = cb.Name;
                    evt.Start = new iCalDateTime(evtStart);
                    evt.End = new iCalDateTime(evtEnd);
                    evt.LastModified = new iCalDateTime(DateTime.Now);
                }


            }//end if

            //add all the events to the calendar 
            foreach (var e in events)
            {
                DDay.iCal.Event evt = courseCalendar.Create<DDay.iCal.Event>();

                evt.Start = new iCalDateTime(e.StartDate.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                if (e.EndDate == null)
                {
                    evt.End = new iCalDateTime(e.StartDate.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                    evt.End.AddMinutes(1);
                }
                else
                {
                    evt.End = new iCalDateTime(((DateTime)e.EndDate).UTCToCourse(ActiveCourseUser.AbstractCourseID));
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
        public ActionResult DownloadCourseCalendar()
        {
            //Get the Course
            Course course = ActiveCourseUser.AbstractCourse as Course;

            //get course events
            var events = (from e in db.Events
                          where e.Poster.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                          && e.StartDate >= course.StartDate
                          && e.StartDate <= course.EndDate
                          && e.Approved
                          select new
                          {
                              e.StartDate,
                              e.EndDate,
                              e.Title,
                              e.Description
                          }).ToList();

            //Create the calendar object 
            iCalendar courseCalendar = new iCalendar();
            //initalize the Calendar object 
            courseCalendar.AddTimeZone(DateTimeExtensions.GetTimeZone(((Course) ActiveCourseUser.AbstractCourse).TimeZoneOffset));
            courseCalendar.Method = "PUBLISH";
            courseCalendar.Name = "VCALENDAR";
            courseCalendar.Version = "2.0";
            courseCalendar.ProductID = "-//Washington State University//OSBLE.org//EN";
            courseCalendar.Scale = "GREGORIAN";
            courseCalendar.AddProperty("X-WR-CALNAME", course.Prefix + "-" + course.Number + "-" + course.Semester + "-" + course.Year);

            //Creating the patterns for course meetings 
            if (ActiveCourseUser.AbstractCourse is Course && ((ActiveCourseUser.AbstractCourse as Course).ShowMeetings == true))
            {

                foreach (CourseMeeting cm in course.CourseMeetings)
                {
                    StringBuilder rpPattern = new StringBuilder("FREQ=WEEKLY;UNTIL=");
                    rpPattern.Append(new iCalDateTime(course.EndDate.UTCToCourse(ActiveCourseUser.AbstractCourseID)).ToString(@"yyyyMMdd\THHmmss\Z"));
                    rpPattern.Append(";WKST=SU;BYDAY=");
                    if (cm.Sunday)
                        rpPattern.Append("SU,");
                    if (cm.Monday)
                        rpPattern.Append("MO,");
                    if (cm.Tuesday)
                        rpPattern.Append("TU,");
                    if (cm.Wednesday)
                        rpPattern.Append("WE,");
                    if (cm.Thursday)
                        rpPattern.Append("TH,");
                    if (cm.Friday)
                        rpPattern.Append("FR,");
                    if (cm.Saturday)
                        rpPattern.Append("SA");

                    //trim trailing comma if it is there
                    if (rpPattern[rpPattern.Length - 1] == ',')
                        rpPattern.Remove(rpPattern.Length - 1, 1);

                    RecurringComponent recurringComponent = new RecurringComponent();
                    RecurrencePattern pattern = new RecurrencePattern(rpPattern.ToString());

                    DDay.iCal.Event evt = courseCalendar.Create<DDay.iCal.Event>();
                    //may cause issues
                    DateTime evtStart = DateTime.SpecifyKind(course.StartDate, DateTimeKind.Utc);
                    evtStart = evtStart.Add(cm.StartTime.TimeOfDay);
                    DateTime evtEnd = DateTime.SpecifyKind(course.StartDate, DateTimeKind.Utc);
                    evtEnd = evtEnd.Add(cm.EndTime.TimeOfDay);

                    //we need to test if right NOW is in DST.
                    //case: course starts during DST, and it's now after DST, result: after DST event is now wrong.
                    DateTime currentTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day,
                                                        evtStart.Hour, evtStart.Minute, evtStart.Second, DateTimeKind.Utc);
                    bool isDaylight = TimeZoneInfo.Local.IsDaylightSavingTime(currentTime.UTCToCourse(ActiveCourseUser.AbstractCourseID));

                    if (isDaylight)
                    {
                        evt.Start = new iCalDateTime(evtStart.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                        evt.End = new iCalDateTime(evtEnd.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                        evt.LastModified = new iCalDateTime(DateTime.Now);
                        evt.Summary = cm.Name;
                        evt.Location = cm.Location;
                        evt.RecurrenceRules.Add(pattern);
                    }
                    else //no longer DST
                    {
                        bool matchingStartDay = false;
                        while (!matchingStartDay)
                        {
                            switch (currentTime.DayOfWeek)
                            {
                                case DayOfWeek.Monday:
                                    if (cm.Monday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Tuesday:
                                    if (cm.Tuesday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Wednesday:
                                    if (cm.Wednesday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Thursday:
                                    if (cm.Thursday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Friday:
                                    if (cm.Friday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Saturday:
                                    if (cm.Saturday)
                                        matchingStartDay = true;
                                    break;
                                case DayOfWeek.Sunday:
                                    if (cm.Sunday)
                                        matchingStartDay = true;
                                    break;
                                default:
                                    matchingStartDay = false;
                                    break;
                            }

                            if (!matchingStartDay)
                            {
                                currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.AddDays(1).Day,
                                                        evtStart.Hour, evtStart.Minute, evtStart.Second, DateTimeKind.Utc);
                            }
                        }

                        evt.Start = new iCalDateTime(currentTime.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                        evt.End = new iCalDateTime(evtEnd.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                        evt.LastModified = new iCalDateTime(DateTime.Now);
                        evt.Summary = cm.Name;
                        evt.Location = cm.Location;
                        evt.RecurrenceRules.Add(pattern);
                    }

                }// end foreach

                //create the course breaks 
                foreach (CourseBreak cb in course.CourseBreaks)
                {
                    DDay.iCal.Event evt = courseCalendar.Create<DDay.iCal.Event>();
                    DateTime evtStart = cb.StartDate.Date;
                    DateTime evtEnd = cb.EndDate.Date.AddDays(1);

                    evt.Summary = cb.Name;
                    evt.Start = new iCalDateTime(evtStart);
                    evt.End = new iCalDateTime(evtEnd);
                    evt.LastModified = new iCalDateTime(DateTime.Now);
                }


            }//end if


            //add all the events to the calendar 
            foreach (var e in events)
            {
                DDay.iCal.Event evt = courseCalendar.Create<DDay.iCal.Event>();

                evt.Start = new iCalDateTime(e.StartDate.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                if (e.EndDate == null)
                {
                    evt.End = new iCalDateTime(e.StartDate.UTCToCourse(ActiveCourseUser.AbstractCourseID));
                    evt.End.AddMinutes(1);

                }
                else
                {
                    evt.End = new iCalDateTime(((DateTime)e.EndDate).UTCToCourse(ActiveCourseUser.AbstractCourseID));
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

            return File(bytes, contentType, course.Prefix + course.Number + "-" + course.Semester + "-" + course.Year + ".ics");

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
            Course course = ActiveCourseUser.AbstractCourse as Course;
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
            var course = (from d in db.Courses
                          where d.ID == id
                          select new
                          {
                              d.ID,
                              d.Prefix,
                              d.Number,
                              d.Semester,
                              d.Year
                          }).FirstOrDefault();

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
