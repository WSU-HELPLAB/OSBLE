namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class AskForHelpEvent : ActivityEvent
    {
        public string Code { get; set; }
        public string UserComment { get; set; }

        public AskForHelpEvent() { } // NOTE!! This is required by Dapper ORM

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId) VALUES ({0}, '{1}', {2})
INSERT INTO dbo.AskForHelpEvents (EventLogId, EventDate, SolutionName, Code, UserComment)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}', '{4}', '{5}')", EventTypeId, EventDate, SenderId, SolutionName, Code, UserComment);
        }
    }
}
