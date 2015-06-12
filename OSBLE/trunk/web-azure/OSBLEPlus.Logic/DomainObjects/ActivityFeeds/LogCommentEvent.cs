using System.Collections.Generic;

using OSBLEPlus.Logic.DomainObjects.Interfaces;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class LogCommentEvent : ActivityEvent
    {
        public int SourceEventLogId { get; set; }
        public IActivityEvent SourceEvent { get; set; }
        public string Content { get; set; }
        public int NumberHelpfulMarks { get; set; }
        public LogCommentEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
