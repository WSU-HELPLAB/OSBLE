using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class HelpfulMarkGivenEvent : ActivityEvent
    {
        public int LogCommentEventId { get; set; }
        public LogCommentEvent LogComment { get; set; }

        public HelpfulMarkGivenEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)Utility.Lookups.EventType.HelpfulMarkGivenEvent;
        }

        public HelpfulMarkGivenEvent(DateTime dateTimeValue)
            : this()
        {
            EventDate = dateTimeValue;
        }

        public override string GetInsertScripts()
        {
            string batchString = BatchId == null ? "NULL" : BatchId.ToString();
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {5})
INSERT INTO dbo.HelpfulMarkGivenEvents (EventLogId, LogCommentEventId, EventDate, SolutionName)
VALUES (SCOPE_IDENTITY(), {3}, '{1}', '{4}')", EventTypeId, EventDate, SenderId, LogCommentEventId, SolutionName, batchString);
        }
    }
}
