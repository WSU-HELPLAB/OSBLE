using System;
using System.Linq;
using System.Web.Http;

using OSBLEPlus.Logic.DataAccess.Analytics;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Services.Attributes;

namespace OSBLEPlus.Services.Controllers
{
    public class CalendarController : ApiController
    {
        /// <summary>
        /// Takes a CalendarAttributes class, returns the Daily Aggregation from the calendar
        /// </summary>
        /// <param name="attr">
        /// attr has the following POCO format
        /// public DateTime ReferenceDate { get; set; }
        /// public AggregateFunction AggregateFunctionId { get; set; }
        /// public int? CourseId { get; set; }
        /// public string SelectedMeasures { get; set; }
        /// public string SubjectUsers { get; set; }
        /// </param>
        /// <returns></returns>
        [AllowAdmin]
        public DailyAggregations Get([FromUri]CalendarAttributes attr)
        {
            return Calendar.GetDailyAggregates
                                    (
                                        attr.ReferenceDate, // start date
                                        attr.ReferenceDate.AddMonths(2), // end date
                                        string.IsNullOrWhiteSpace(attr.SubjectUsers) ? null : attr.SubjectUsers.Split(',').Select(x => Convert.ToInt32(x)).ToList(), // csv user ids
                                        attr.CourseId ?? -1, // course id
                                        attr.SelectedMeasures, // csv measures
                                        attr.AggregateFunctionId == AggregateFunction.Avg // sum total or average aggregation
                                    );
        }
    }
}
