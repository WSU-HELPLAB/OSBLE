using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class DebugEvent : ActivityEvent
    {
        public int ExecutionAction { get; set; }

        public string DocumentName { get; set; }

        public int LineNumber { get; set; }
        public string DebugOutput { get; set; }

        public DebugEvent()
        {
            DebugOutput = string.Empty;
            LineNumber = -1;
            EventTypeId = (int)Utility.Lookups.EventType.DebugEvent;
        }

        public DebugEvent(DateTime dateTimeValue)
            : this()
        {
            EventDate = dateTimeValue;
        }

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {8})
INSERT INTO dbo.DebugEvents (EventLogId, EventDate, SolutionName, ExecutionAction, DocumentName, LineNumber, DebugOutput)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}', {4}, '{5}', {6}, '{7}')", EventTypeId, EventDate, SenderId, SolutionName, ExecutionAction, DocumentName, LineNumber, DebugOutput, BatchId);
        }
    }
}
