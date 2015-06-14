using System;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.Interfaces
{
    public interface IEventLog
    {
        int EventLogId { get; set; }
        int EventTypeId { get; set; }
        EventType EventType { get; }
        DateTime EventDate { get; set; }
        int SenderId { get; set; }
        IUser Sender { get; set; }
    }
}
