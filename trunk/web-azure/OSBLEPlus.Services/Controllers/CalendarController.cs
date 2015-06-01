using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

using OSBLEPlus.Logic.DataAccess.Analytics;
using OSBLEPlus.Logic.DomainObjects;
using OSBLEPlus.Logic.DomainObjects.Analytics;

namespace OSBLEPlus.Services.Controllers
{
    [EnableCors(origins: "http://localhost:8088", headers: "*", methods: "*")]
    public class CalendarController : ApiController
    {
        public DailyAggregations Get(CalendarAttributes attr)
        {
            return Calendar.GetDailyAggregates
                                    (
                                        attr.ReferenceDate, // start date
                                        attr.ReferenceDate.AddMonths(2), // end date
                                        string.IsNullOrWhiteSpace(attr.SubjectUsers) ? null : attr.SubjectUsers.Split(',').Select(x => Convert.ToInt32(x)).ToList(), // csv user ids
                                        attr.CourseId ?? 99, // course id
                                        attr.SelectedMeasures, // csv measures
                                        attr.AggregateFunctionId == AggregateFunction.Avg // sum total or average aggregation
                                    );
        }
    }
}
