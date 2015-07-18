using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public sealed class LogCommentEvent : ActivityEvent
    {
        public int SourceEventLogId { get; set; }
        public IActivityEvent SourceEvent { get; set; }
        public string Content { get; set; }
        public int NumberHelpfulMarks { get; set; }
        public LogCommentEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)Utility.Lookups.EventType.LogCommentEvent;
        }

        public LogCommentEvent(DateTime dateTimeValue)
            : this()
        {
            EventDate = dateTimeValue;
        }

        public override string GetInsertScripts()
        {
            string batchString = BatchId == null ? "NULL" : BatchId.ToString();

            string s = string.Format(@"
                INSERT INTO dbo.EventLogs (EventTypeId, EventDate, SenderId, BatchId, CourseId, SolutionName) VALUES ({0}, '{1}', {2}, {6}, {7}, '{4}')
                INSERT INTO dbo.LogCommentEvents (EventLogId,SourceEventLogId,EventDate,SolutionName,Content)
                VALUES (SCOPE_IDENTITY(),{3}, '{1}', '{4}', '{5}')", EventTypeId, EventDate, SenderId, SourceEventLogId, SolutionName, Content.Replace("'", "''"), batchString, CourseId);

            return s;
        }

        public static string Name { get { return "LogCommentEvent"; } }
    }
}
