using System.Collections.Generic;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public class HourlyAggregations
    {
        public List<HourlyMeasures> Measures { get; set; }
        public int Max { get; set; }
    }

    public class HourlyMeasures
    {
        public string Title { get; set; }
        public string Color { get; set; }
        public List<HourlyMeasure> Values { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public double Avg { get; set; }
    }

    public class HourlyMeasure
    {
        public int Hour { get; set; }
        public double Value { get; set; }
    }
}