namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class HelpfulMarkGivenEvent: ActivityEvent
    {
        public int LogCommentEventId { get; set; }
        public LogCommentEvent LogComment { get; set; }
        public HelpfulMarkGivenEvent() { } // NOTE!! This is required by Dapper ORM

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId) VALUES ({0}, '{1}', {2})
INSERT INTO dbo.HelpfulMarkGivenEvents (EventLogId, LogCommentEventId, EventDate, SolutionName)
VALUES (SCOPE_IDENTITY(), {3}, '{1}', '{4}')", EventTypeId, EventDate, SenderId, LogCommentEventId, SolutionName);
        }
    }
}
