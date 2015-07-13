namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public sealed class HelpfulMarkGivenEvent : ActivityEvent
    {
        public int LogCommentEventId { get; set; }
        public LogCommentEvent LogComment { get; set; }

        public HelpfulMarkGivenEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)Utility.Lookups.EventType.HelpfulMarkGivenEvent;
        }

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {5})
INSERT INTO dbo.HelpfulMarkGivenEvents (EventLogId, LogCommentEventId, EventDate, SolutionName)
VALUES (SCOPE_IDENTITY(), {3}, '{1}', '{4}')", EventTypeId, EventDate, SenderId, LogCommentEventId, SolutionName, BatchId);
        }
    }
}
