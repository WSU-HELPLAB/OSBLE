using System;
using OSBLE.Controllers;
using OSBLE.Models;
using OSBLE.Models.Courses;

public static class DateTimeExtensions
{
    public static DateTime CourseToUTC(this DateTime date, int courseID)
    {
        OSBLEContext db = new OSBLEContext();
        int offsetVal = ((Course) db.AbstractCourses.Find(courseID)).TimeZoneOffset;
        CourseController cc = new CourseController();
        TimeZoneInfo tzInfo = cc.getTimeZone(offsetVal);

        return TimeZoneInfo.ConvertTimeToUtc(date, tzInfo);
    }
    public static DateTime UTCToCourse(this DateTime date, int courseID)
    {
        OSBLEContext db = new OSBLEContext();
        int offsetVal = ((Course)db.AbstractCourses.Find(courseID)).TimeZoneOffset;
        CourseController cc = new CourseController();
        TimeZoneInfo tzInfo = cc.getTimeZone(offsetVal);

        return TimeZoneInfo.ConvertTimeFromUtc(date, tzInfo);
    }
}

