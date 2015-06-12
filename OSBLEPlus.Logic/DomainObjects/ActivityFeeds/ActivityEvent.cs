using System;

using OSBLEPlus.Logic.DomainObjects;
using OSBLEPlus.Logic.DomainObjects.Interfaces;
using OSBLEPlus.Logic.DomainObjects.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class ActivityEvent : IActivityEvent
    {
        public int EventLogId { get; set; }
        public int EventTypeId { get; set; }
        public EventType EventType { get { return (EventType) EventTypeId; } }
        public DateTime EventDate { get; set; }
        public int SenderId { get; set; }
        public IUser Sender { get; set; }
        public int EventId { get; set; }
        public string EventName {
            get { return EventType.ToString().ToDisplayText(); }
        }
        public string SolutionName { get; set; }

        public ActivityEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
