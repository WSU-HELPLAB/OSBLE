using System.Collections.Generic;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public class Aggregate
    {
        public int Month { get; set; }
        public int Day { get; set; }
        public int Value { get; set; }
    }

    public class Activity
    {
        public int Month { get; set; }
        public int Day { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// aggregates of a calendar day of a measure
    /// </summary>
    public class Measure
    {
        public string Title { get;set; }
        public char DataPointShape { get;set; }
        public string Color { get; set; }

        public int FirstDataPointMonth { get; set; }
        public int FirstDataPointDay { get; set; }
        public int LastDataPointMonth { get; set; }
        public int LastDataPointDay { get; set; }
        public List<Aggregate> Aggregates { get; set; }

        /// <summary>
        /// min max avg of the measure
        /// </summary>
        public double Max { get; set; }
        public double Min { get; set; }
        public double Avg { get; set; }
    }

    public class DailyAggregations
    {
        public List<Measure> Measures { get; set; }
        public List<Activity> Activities { get; set; }
    }
}