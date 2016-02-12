using System;
using System.Data.SqlClient;
using OSBLEPlus.Logic.Utility;

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

        public override SqlCommand GetInsertCommand()
        {
            var cmd = new SqlCommand
            {
                CommandText = string.Format(@"
DECLARE {0} INT
INSERT INTO dbo.EventLogs (EventTypeId, EventDate, SenderId, CourseId) VALUES (@EventTypeId, @EventDate, @SenderId, @CourseId)
SELECT {0}=SCOPE_IDENTITY()
INSERT INTO dbo.DebugEvents (EventLogId, EventDate, SolutionName, ExecutionAction, DocumentName, LineNumber, DebugOutput)
VALUES ({0}, @EventDate, @SolutionName, @ExecutionAction, @DocumentName, @LineNumber, @DebugOutput)
SELECT {0}", StringConstants.SqlHelperLogIdVar)
            };
            cmd.Parameters.AddWithValue("EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("EventDate", EventDate);
            cmd.Parameters.AddWithValue("SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("SolutionName", SolutionName);
            cmd.Parameters.AddWithValue("ExecutionAction", ExecutionAction);
            cmd.Parameters.AddWithValue("DocumentName", DocumentName);
            cmd.Parameters.AddWithValue("LineNumber", LineNumber);
            cmd.Parameters.AddWithValue("DebugOutput", DebugOutput);

            return cmd;
        }
    }
}
