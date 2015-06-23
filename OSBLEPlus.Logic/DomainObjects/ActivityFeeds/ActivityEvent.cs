using System;

using OSBLEPlus.Logic.DomainObjects.Interfaces;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class ActivityEvent : IActivityEvent
    {
        // EventLogs table contents
        public int EventLogId { get; set; }
        public int EventTypeId { get; set; }
        public EventType EventType
        {
            get { return (EventType) EventTypeId; }
        }
        public DateTime EventDate { get; set; }
        public int SenderId { get; set; }
        public IUser Sender { get; set; }

        // Detailed events table contents
        public int EventId { get; set; }
        public string EventName
        {
            get { return EventType.ToString().ToDisplayText(); }
        }
        public string SolutionName { get; set; }
        public int? CourseId { get; set; }
        public long? BatchId { get; set; }

        // Helper method to efficiently generate TSQL insert scripts
        // Don't use property, since the activities need to be serialized to go across the wire
        // Can't use abstract since Dapper ORM needs to instantiate instances of the class
        public virtual string GetInsertScripts()
        {
            return string.Empty;
        }

        public ActivityEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
