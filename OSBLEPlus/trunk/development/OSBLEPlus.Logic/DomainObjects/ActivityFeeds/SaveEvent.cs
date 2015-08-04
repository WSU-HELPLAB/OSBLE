using System;
using System.Data.SqlClient;

using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class SaveEvent : ActivityEvent
    {
        public int DocumentId { get; set; }
        public CodeDocument Document { get; set; }
        public SaveEvent()
        {
            EventTypeId = (int)EventType.SaveEvent;
        }

        public SaveEvent(DateTime dateTimeValue)
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
DECLARE {1} INT
INSERT INTO dbo.EventLogs (EventTypeId, EventDate, SenderId, CourseId)
VALUES (@EventTypeId, @EventDate, @SenderId, @CourseId)
SELECT {0}=SCOPE_IDENTITY()
INSERT INTO dbo.CodeDocuments([FileName],[Content]) VALUES (@FileName, @Content)
SELECT {1}=SCOPE_IDENTITY()
INSERT INTO dbo.SaveEvents (EventLogId, EventDate, SolutionName, DocumentId)
VALUES ({0}, @EventDate, @SolutionName, {1})
SELECT {0}", StringConstants.SqlHelperLogIdVar, StringConstants.SqlHelperDocIdVar)
            };

            cmd.Parameters.AddWithValue("EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("EventDate", EventDate);
            cmd.Parameters.AddWithValue("SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("SolutionName", SolutionName);
            cmd.Parameters.AddWithValue("FileName", Document.FileName);
            cmd.Parameters.AddWithValue("Content", Document.Content);

            return cmd;
        }
    }
}
