using System;
using System.Data.SqlClient;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
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

        public override SqlCommand GetInsertCommand()
        {
            var cmd = new SqlCommand
            {
                CommandText = string.Format(@"
DECLARE {0} INT
INSERT INTO dbo.EventLogs (EventTypeId, EventDate, SenderId, CourseId) VALUES (@EventTypeId, @EventDate, @SenderId, @CourseId)
SELECT {0}=SCOPE_IDENTITY()
INSERT INTO dbo.CutCopyPasteEvents (EventLogId, EventDate, SolutionName, EventAction, DocumentName, Content)
VALUES ({0}, @EventDate, @SolutionName, @EventAction, @DocumentName, @Content)
SELECT {0}", StringConstants.SqlHelperLogIdVar)
            };

            cmd.Parameters.AddWithValue("EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("EventDate", EventDate);
            cmd.Parameters.AddWithValue("SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("SolutionName", SolutionName);
            cmd.Parameters.AddWithValue("EventAction", EventActionId);
            cmd.Parameters.AddWithValue("DocumentName", DocumentName);
            cmd.Parameters.AddWithValue("Content", Content);

            return cmd;
        }
    }
}
