using System;
using System.Data.SqlClient;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class AskForHelpEvent : ActivityEvent
    {
        public string Code { get; set; }
        public string UserComment { get; set; }

        public AskForHelpEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)EventType.AskForHelpEvent;
        }

        public AskForHelpEvent(DateTime dateTimeValue)
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
INSERT INTO dbo.EventLogs (EventTypeId, EventDate, SenderId, CourseId, Isanonymous) VALUES (@EventTypeId, @EventDate, @SenderId, @CourseId, @IsAnonymous)
SELECT {0}=SCOPE_IDENTITY()
INSERT INTO dbo.AskForHelpEvents (EventLogId, EventDate, SolutionName, Code, UserComment)
VALUES ({0}, @EventDate, @SolutionName, @Code, @UserComment)
SELECT {0}", StringConstants.SqlHelperLogIdVar)
            };
            cmd.Parameters.AddWithValue("EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("EventDate", EventDate);
            cmd.Parameters.AddWithValue("SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("SolutionName", SolutionName);
            cmd.Parameters.AddWithValue("Code", Code);
            cmd.Parameters.AddWithValue("UserComment", UserComment);
            cmd.Parameters.AddWithValue("IsAnonymous", IsAnonymous);
            return cmd;
        }
    }
}
