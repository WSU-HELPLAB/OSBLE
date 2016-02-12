using System;
using System.Collections.Generic;
using System.Dynamic;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public class Point
    {
        public string Name { get; set; }
        public double Position { get; set; }

        public DateTime EventTime { get; set; }

        public string EventTimeDisplayText
        {
            get
            {
                // for datatime picker to init the popup calendar values, local time
                return string.Format("{0}/{1}/{2} {3}:{4}",
                    EventTime.Year, EventTime.Month, EventTime.Day, EventTime.Hour, EventTime.Minute);
            }
        }
    }
    public class State
    {
        // the top 3 properties are used internally for calculating timeout idle state
        internal ProgrammingState ProgrammingState { get; set; }
        internal DateTime ShiftedStartTime { get; set; }

        public string StartTimeDisplayText
        {
            get
            {
                // for datatime picker to init the popup calendar values, local time
                return string.Format("{0}/{1}/{2} {3}:{4}",
                    StartTime.Year, StartTime.Month, StartTime.Day, StartTime.Hour, StartTime.Minute);
            }
        }

        public string EndTimeDisplayText
        {
            get
            {
                // for datatime picker to init the popup calendar values, local time
                return string.Format("{0}/{1}/{2} {3}:{4}",
                    EndTime.Year, EndTime.Month, EndTime.Day, EndTime.Hour, EndTime.Minute);
            }
        }
        // for rectangle state duration message, utc matches d3 format
        public string TimeRangeDisplayText { get { return string.Format("{0} - {1}", StartTime.ToUniversalTime().ToString("g"), EndTime.ToUniversalTime().ToString("g")); } }
        // plotting and displaying properties
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public double StartPoint { get; set; }
        public long StartTicks
        {
            get { return StartTime.Ticks/10000; }
        }
        public DateTime EndTime { get; set; }
        public double EndPoint { get; set; }
        public long EndTicks
        {
            get { return EndTime.Ticks / 10000; }
        }
        public string CssClass { get; set; }
    }
    public class TimelineChartData
    {
        public int UserId { get; set; }
        public string title { get; set; }
        public List<State> measures { get; set; }
        public List<Point> markers { get; set; }
        public bool showTicks { get; set; }
    }

    public class StudentData
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
