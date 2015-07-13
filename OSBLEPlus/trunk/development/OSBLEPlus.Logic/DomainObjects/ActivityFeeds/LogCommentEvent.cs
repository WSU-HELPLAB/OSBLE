using OSBLEPlus.Logic.DomainObjects.Interface;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public sealed class LogCommentEvent : ActivityEvent
    {
        public int SourceEventLogId { get; set; }
        public IActivityEvent SourceEvent { get; set; }
        public string Content { get; set; }
        public int NumberHelpfulMarks { get; set; }
        public LogCommentEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)Utility.Lookups.EventType.LogCommentEvent;
        }

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {6})
INSERT INTO dbo.LogCommentEvents (EventLogId,SourceEventLogId,EventDate,SolutionName,Content)
VALUES (SCOPE_IDENTITY(),{3}, '{1}', '{4}', '{5}')", EventTypeId, EventDate, SenderId, SourceEventLogId, SolutionName, Content.Replace("'", "''"), BatchId);
        }
    }
}
