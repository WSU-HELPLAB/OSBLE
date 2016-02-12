using System;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public class CalendarAttributes
    {
        public DateTime ReferenceDate { get; set; }
        public AggregateFunction AggregateFunctionId { get; set; }
        public int? CourseId { get; set; }
        public string SelectedMeasures { get; set; }
        public string SubjectUsers { get; set; }
    }
}