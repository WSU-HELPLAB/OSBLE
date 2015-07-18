using System;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public sealed class CutCopyPasteEvent : ActivityEvent
    {
        public int EventActionId { get; set; }

        public CutCopyPasteActions Action
        {
            get { return (CutCopyPasteActions)EventActionId; }
        }

        public string DocumentName { get; set; }

        public string Content { get; set; }

        public CutCopyPasteEvent()
        {
            DocumentName = string.Empty;
            Content = string.Empty;
            EventTypeId = (int)EventType.CutCopyPasteEvent;
        }

        public CutCopyPasteEvent(DateTime dateTimeValue)
            : this()
        {
            EventDate = dateTimeValue;
        }

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {7})
INSERT INTO dbo.CutCopyPasteEvents (EventLogId, EventDate, SolutionName, EventAction, DocumentName, Content)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}', {4}, '{5}', '{6}')", EventTypeId, EventDate, SenderId, SolutionName, EventActionId, DocumentName, Content.Replace("'", "''"), BatchId);
        }
    }
}
