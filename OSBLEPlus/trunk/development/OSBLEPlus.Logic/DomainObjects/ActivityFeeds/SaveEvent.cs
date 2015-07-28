using System;
using System.Text;

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

        public override string GetInsertScripts()
        {
            var sql =
                new StringBuilder(
                    string.Format(
                        @"INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {3})"
                        , EventTypeId, EventDate, SenderId, BatchId));
            sql.AppendFormat(@"{0}SELECT {1}=SCOPE_IDENTITY()", Environment.NewLine, StringConstants.SqlHelperLogIdVar);

            sql.AppendFormat(@"{0}INSERT INTO dbo.CodeDocuments([FileName],[Content]) VALUES ('{1}','{2}')", Environment.NewLine, Document.FileName, Document.Content);
            sql.AppendFormat(@"{0}SELECT {1}=SCOPE_IDENTITY()", Environment.NewLine, StringConstants.SqlHelperDocIdVar);

            sql.AppendFormat(@"{0}INSERT INTO dbo.SaveEvents (EventLogId, EventDate, SolutionName, DocumentId) VALUES ({1}, '{2}', '{3}', {4})",
                Environment.NewLine, StringConstants.SqlHelperLogIdVar, EventDate, SolutionName, StringConstants.SqlHelperDocIdVar);

            return sql.ToString();
        }
    }
}
