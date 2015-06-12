using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

using OSBLEPlus.Logic.DataAccess.Analytics;
using OSBLEPlus.Logic.DomainObjects;
using OSBLEPlus.Logic.DomainObjects.Analytics;

namespace OSBLEPlus.Services.Controllers
{
    [EnableCors(origins: "http://localhost:9000", headers: "*", methods: "*")]
    public class CalendarController : ApiController
    {
        public DailyAggregations GetMeasures(AggregateFunction a, int c, DateTime s, DateTime e, string m, string users)
        {
            return Calendar.GetDailyAggregates
                                    (
                                        s, // start date
                                        e, // end data
                                        string.IsNullOrWhiteSpace(users) ? null : users.Split(',').Select(x=>Convert.ToInt32(x)).ToList(), // csv user ids
                                        c, // course id
                                        m, // csv measures
                                        a == AggregateFunction.Avg // sum total or average aggregation
                                    );
        }

        public HourlyAggregations GetHourlyMeasures(AggregateFunction a, int c, int y, int n, int d, string m, string users)
        {
            return Calendar.GetHourlyAggregates
                                    (
                                        new DateTime(y, n, d),
                                        string.IsNullOrWhiteSpace(users) ? null : users.Split(',').Select(x => Convert.ToInt32(x)).ToList(), // csv user ids
                                        c, // course id
                                        m, // csv measures
                                        a == AggregateFunction.Avg // sum total or average aggregation
                                    );
        }
    }
}
