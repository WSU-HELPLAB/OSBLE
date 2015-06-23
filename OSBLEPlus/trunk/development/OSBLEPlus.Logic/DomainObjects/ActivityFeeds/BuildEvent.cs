namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class BuildEvent : ActivityEvent
    {
        public string CriticalErrorName { get; set; }

        public BuildEvent() { } // NOTE!! This is required by Dapper ORM

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId) VALUES ({0}, '{1}', {2}, {4})
INSERT INTO dbo.BuildEvents (EventLogId, EventDate, SolutionName)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}')", EventTypeId, EventDate, SenderId, SolutionName, BatchId);
        }
    }
}
