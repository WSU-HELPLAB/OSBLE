using System;
using System.Data.SqlClient;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class HelpfulMarkGivenEvent : ActivityEvent
    {
        public int LogCommentEventId { get; set; }
        public LogCommentEvent LogComment { get; set; }

        public HelpfulMarkGivenEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)Utility.Lookups.EventType.HelpfulMarkGivenEvent;
        }

        public HelpfulMarkGivenEvent(DateTime dateTimeValue)
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
INSERT INTO dbo.HelpfulMarkGivenEvents (EventLogId, LogCommentEventId, EventDate, SolutionName)
VALUES ({0}, @LogCommentEventId, @EventDate, @SolutionName)
SELECT {0}", StringConstants.SqlHelperLogIdVar)
            };
            cmd.Parameters.AddWithValue("EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("EventDate", EventDate);
            cmd.Parameters.AddWithValue("SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("LogCommentEventId", LogCommentEventId);
            cmd.Parameters.AddWithValue("SolutionName", SolutionName);

            return cmd;
        }
    }
}
