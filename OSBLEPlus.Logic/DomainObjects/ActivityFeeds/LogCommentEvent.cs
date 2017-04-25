using System;
using System.Data.SqlClient;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class LogCommentEvent : ActivityEvent
    {
        public int SourceEventLogId { get; set; }
        public IActivityEvent SourceEvent { get; set; }
        public string Content { get; set; }
        public int NumberHelpfulMarks { get; set; }
        public LogCommentEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)EventType.LogCommentEvent;
        }

        public LogCommentEvent(DateTime dateTimeValue)
            : this()
        {
            EventDate = dateTimeValue;
        }

        public override SqlCommand GetInsertCommand()
        {
            var cmd = new SqlCommand();

            var sql = string.Format(@"
DECLARE {0} INT
INSERT INTO dbo.EventLogs (EventTypeId, EventDate, SenderId, CourseId, SolutionName, IsAnonymous)
VALUES (@EventTypeId, @EventDate, @SenderId, @CourseId, @SolutionName, @IsAnonymous)
SELECT {0}=SCOPE_IDENTITY()
INSERT INTO dbo.LogCommentEvents (EventLogId,SourceEventLogId,EventDate,SolutionName,Content)
VALUES ({0}, @SourceEventLogId, @EventDate, @SolutionName,@Content)
SELECT {0}", StringConstants.SqlHelperLogIdVar);

            cmd.Parameters.AddWithValue("@EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("@EventDate", EventDate);
            cmd.Parameters.AddWithValue("@SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("@SolutionName", SolutionName);
            cmd.Parameters.AddWithValue("@SourceEventLogId", SourceEventLogId);
            cmd.Parameters.AddWithValue("@Content", Content);
            cmd.Parameters.AddWithValue("@IsAnonymous", IsAnonymous);

            cmd.CommandText = sql;

            return cmd;
        }

        public static string Name { get { return "LogCommentEvent"; } }
    }
}
