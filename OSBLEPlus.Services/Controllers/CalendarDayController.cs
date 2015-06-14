using System;
using System.Linq;
using System.Web.Http;

using OSBLEPlus.Logic.DataAccess.Analytics;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Services.Attributes;

namespace OSBLEPlus.Services.Controllers
{
    public class CalendarDayController : ApiController
    {
        [AllowAdmin]
        public HourlyAggregations Get([FromUri]CalendarAttributes attr)
        {
            return Calendar.GetHourlyAggregates
                                    (
                                        attr.ReferenceDate,
                                        string.IsNullOrWhiteSpace(attr.SubjectUsers) ? null : attr.SubjectUsers.Split(',').Select(x => Convert.ToInt32(x)).ToList(), // csv user ids
                                        attr.CourseId ?? 99, // course id
                                        attr.SelectedMeasures, // csv measures
                                        attr.AggregateFunctionId == AggregateFunction.Avg // sum total or average aggregation
                                    );
        }
    }
}
