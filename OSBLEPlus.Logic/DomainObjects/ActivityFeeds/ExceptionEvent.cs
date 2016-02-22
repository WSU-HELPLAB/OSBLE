using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class ExceptionEvent : ActivityEvent
    {
        public string DocumentName { get; set; }
        public int ExceptionAction { get; set; }
        public int ExceptionCode { get; set; }
        public string ExceptionDescription { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionName { get; set; }
        public string LineContent { get; set; }
        public int LineNumber { get; set; }
        public IList<StackFrame> StackFrames { get; set; }

        public ExceptionEvent() // NOTE!! This is required by Dapper ORM
        {
            StackFrames = new List<StackFrame>();
            EventTypeId = (int)Utility.Lookups.EventType.ExceptionEvent;
        }

        public ExceptionEvent(DateTime dateTimeValue)
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
INSERT INTO dbo.ExceptionEvents (EventLogId,EventDate,SolutionName,ExceptionType,ExceptionName,ExceptionCode,ExceptionDescription,ExceptionAction,DocumentName,LineNumber,LineContent)
VALUES ({0}, @EventDate, @SolutionName, @ExceptionType,@ExceptionName,@ExceptionCode,@ExceptionDescription,@ExceptionAction,@DocumentName,@LineNumber,@LineContent)
SELECT {0}", StringConstants.SqlHelperLogIdVar)
            };
            cmd.Parameters.AddWithValue("EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("EventDate", EventDate);
            cmd.Parameters.AddWithValue("SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("SolutionName", SolutionName);
            cmd.Parameters.AddWithValue("ExceptionType", ExceptionType);
            cmd.Parameters.AddWithValue("ExceptionName", ExceptionName);
            cmd.Parameters.AddWithValue("ExceptionCode", ExceptionCode);
            cmd.Parameters.AddWithValue("ExceptionDescription", ExceptionDescription);
            cmd.Parameters.AddWithValue("ExceptionAction", ExceptionAction);
            cmd.Parameters.AddWithValue("DocumentName", DocumentName);
            cmd.Parameters.AddWithValue("LineNumber", LineNumber);
            cmd.Parameters.AddWithValue("LineContent", LineContent);

            return cmd;
        }

        /// <summary>
        /// Attempt to make exception event view code work. Code was
        /// copied from OSBIDE and requires this function.
        /// </summary>
        /// <returns></returns>
        public List<CodeDocument> GetCodeDocuments()
        {
            List<CodeDocument> docs = new List<CodeDocument>();
            docs.Add(new CodeDocument()
            {
                Id = EventId,
                Content = LineContent,
                FileName = DocumentName
            });
            return docs;
        }

        public BuildEvent GetBuildEvent()
        {
            BuildEvent bEvent = new BuildEvent();
            return bEvent;
        }
    }
}
