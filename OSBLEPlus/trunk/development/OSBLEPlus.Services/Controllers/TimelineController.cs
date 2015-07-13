using System.Collections.Generic;
using System.Web.Http;

using OSBLEPlus.Logic.DataAccess.Analytics;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Services.Attributes;

namespace OSBLEPlus.Services.Controllers
{
    public class TimelineController : ApiController
    {
        [AllowAdmin]
        public List<TimelineChartData> GetTimeline([FromUri] TimelineCriteria criteria)
        {
            return TimelineVisualization.Get
                (
                    criteria, true
                );
        }
    }
}
