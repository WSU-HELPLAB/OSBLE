using System;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public class TimelineCriteria
    {
        /// <summary>
        /// "Days", "Hours", or "Minutes"
        /// </summary>
        public TimeScale timeScale { get; set; }
        /// <summary>
        /// Start Date
        /// </summary>
        public DateTime? timeFrom { get; set; }
        /// <summary>
        /// End Date
        /// </summary>
        public DateTime? timeTo { get; set; }
        /// <summary>
        /// Nullable int
        /// </summary>
        public int? timeout { get; set; }
        /// <summary>
        /// Boolean Value
        /// </summary>
        public bool grayscale { get; set; }
        /// <summary>
        /// Course Id interger
        /// </summary>
        public int courseId { get; set; }
        /// <summary>
        /// String of user Ids, e.g. "1,2,3"
        /// </summary>
        public string userIds { get; set; }
    }
}
