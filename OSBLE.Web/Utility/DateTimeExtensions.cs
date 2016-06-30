using System;
using OSBLE.Controllers;
using OSBLE.Models;
using OSBLE.Models.Courses;

public static class DateTimeExtensions
{
    public static DateTime CourseToUTC(this DateTime date, int? courseID)
    {
        using (var db = new OSBLEContext())
        {
            int offsetVal = -8;
            if(courseID != null)
                    offsetVal = ((Course)db.AbstractCourses.Find(courseID)).TimeZoneOffset;

            TimeZoneInfo tzInfo = GetTimeZone(offsetVal);

            return TimeZoneInfo.ConvertTimeToUtc(date, tzInfo);
        }
    }
    public static DateTime UTCToCourse(this DateTime date, int? courseID)
    {
        using (var db = new OSBLEContext())
        {
            int offsetVal = -8;
            
            if(courseID != null)
            {
                //get Abstract Course
                AbstractCourse abstractCourse = db.AbstractCourses.Find(courseID);
                //check if it's a course or community
                if (abstractCourse is Course)
                {
                    Course course = (Course)abstractCourse;
                    offsetVal = course.TimeZoneOffset;                    
                }                
            }   

            TimeZoneInfo tzInfo = GetTimeZone(offsetVal);

            DateTime utcKind = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utcKind, tzInfo);
        }
    }

    public static TimeZoneInfo GetTimeZone(int tzoffset)
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
            tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        }
        return tz;

    }
}

