using System.Collections.Generic;
using System.Web.Http;

using OSBLEPlus.Logic.DataAccess.Analytics;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Services.Attributes;

namespace OSBLEPlus.Services.Controllers
{
    public class TimelineController : ApiController
    {
        /// <summary>
        /// Gets TimelineChartData from TimelineCriteria
        /// </summary>
        /// <param name="criteria">
        /// criteria has the following POCO entry:
        /// public TimeScale timeScale { get; set; }
        /// public DateTime? timeFrom { get; set; }
        /// public DateTime? timeTo { get; set; }
        /// public int? timeout { get; set; }
        /// public bool grayscale { get; set; }
        /// public int courseId { get; set; }
        /// public string userIds { get; set; }
        /// </param>
        /// <returns>Returns a list of TimelineChartData which has the following POCO entry:
        /// public int UserId { get; set; }
        /// public string title { get; set; }
        /// public List<State> measures { get; set; }
        /// public List<Point> markers { get; set; }
        /// public bool showTicks { get; set; }
        /// </returns>
        [AllowAdmin]
        public List<TimelineChartData> Get([FromUri] TimelineCriteria criteria)
        {
            return TimelineVisualization.Get
                (
                    criteria, true
                );
        }
    }
}
