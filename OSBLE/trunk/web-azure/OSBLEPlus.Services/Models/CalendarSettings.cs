using OSBLEPlus.Logic.DomainObjects;
using OSBLEPlus.Logic.DomainObjects.Analytics;

namespace OSBLEPlus.Services.Models
{
    public class CalendarSettings
    {
        // bi-directional
        public int? CourseId { get; set; }
        public AggregateFunction AggregateFunctionId { get; set; }
        public string SelectedMeasures { get; set; }

        // to client
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }

        // the data area of the calendar
        public DailyAggregations DailyAggregations { get; set; }
        public HourlyAggregations HourlyAggregations { get; set; }

        // from client
        public int MonthOffset { get; set; }
    }
}