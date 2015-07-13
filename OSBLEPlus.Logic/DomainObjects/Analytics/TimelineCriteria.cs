using System;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public class TimelineCriteria
    {

        public TimeScale timeScale { get; set; }
        public DateTime? timeFrom { get; set; }
        public DateTime? timeTo { get; set; }
        public int? timeout { get; set; }
        public bool grayscale { get; set; }

        public int courseId { get; set; }
        public string userIds { get; set; }
    }
}
