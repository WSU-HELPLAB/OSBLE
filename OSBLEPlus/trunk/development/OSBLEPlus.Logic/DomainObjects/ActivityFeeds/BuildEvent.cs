using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class BuildEvent : ActivityEvent
    {
        
        public IList<BuildEventErrorListItem> ErrorItems { get; set; }

        public IList<BuildEventBreakPoint> Breakpoints { get; set; }

        public List<BuildDocument> Documents { get; set; }

        public int CriticalErrorCount
        {
            get
            {
                return CriticalErrorNames.Count;
            }
        }

        private List<string> _criticalErrorNames;

        public List<string> CriticalErrorNames
        {
            get
            {
                if (_criticalErrorNames == null)
                {
                    var query = from item in ErrorItems
                                where !string.IsNullOrEmpty(item.ErrorListItem.CriticalErrorName)
                                select item.ErrorListItem.CriticalErrorName;
                    _criticalErrorNames = query.Distinct().ToList();
                }

                return _criticalErrorNames;
            }
            set
            {
                _criticalErrorNames = value.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
        }

        public List<BuildEventErrorListItem> CriticalErrorItems
        {
            get
            {
                return ErrorItems.Where(errorItem => errorItem.ErrorListItem.Description.StartsWith("error")).ToList();
            }
        }

        public BuildEvent() // NOTE!! This is required by Dapper ORM
        {
            ErrorItems = new List<BuildEventErrorListItem>();
            Breakpoints = new List<BuildEventBreakPoint>();
            Documents = new List<BuildDocument>();
            EventTypeId = (int)Utility.Lookups.EventType.BuildEvent;
        }

        public BuildEvent(DateTime dateTimeValue)
            : this()
        {
            EventDate = dateTimeValue;
        }

        public override SqlCommand GetInsertCommand()
        {
            var cmd = new SqlCommand();

            // the primary sql statement
            var sql = new StringBuilder(string.Format(@"
DECLARE {0} INT
DECLARE {1} INT
DECLARE {2} INT
DECLARE {3} INT
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, CourseId) VALUES (@EventTypeId, @EventDate, @SenderId, @CourseId)
SELECT {0}=SCOPE_IDENTITY()
INSERT INTO dbo.BuildEvents (EventLogId, EventDate, SolutionName)
VALUES ({0}, @EventDate, @SolutionName)
SELECT {1}=SCOPE_IDENTITY()", StringConstants.SqlHelperLogIdVar, StringConstants.SqlHelperEventIdVar, StringConstants.SqlHelperDocIdVar, StringConstants.SqlHelperIdVar));

            // sql param for the primary insert statement
            cmd.Parameters.AddWithValue("EventTypeId", EventTypeId);
            cmd.Parameters.AddWithValue("EventDate", EventDate);
            cmd.Parameters.AddWithValue("SenderId", SenderId);
            if (CourseId.HasValue) cmd.Parameters.AddWithValue("CourseId", CourseId.Value);
            else cmd.Parameters.AddWithValue("CourseId", DBNull.Value);
            cmd.Parameters.AddWithValue("SolutionName", SolutionName);

            int docIdx = 0, errIdx = 0, bkIdx = 0;
            foreach (var doc in from document in Documents where document.Document != null select document.Document)
            {
                docIdx++;

                // sql statement for inserting a code document
                sql.AppendFormat(@"{0}INSERT INTO dbo.CodeDocuments([FileName],[Content]) VALUES (@FileName{1},@Content{1})", Environment.NewLine, docIdx);
                sql.AppendFormat(@"{0}SELECT {1}=SCOPE_IDENTITY()", Environment.NewLine, StringConstants.SqlHelperDocIdVar);
                sql.AppendFormat(@"{0}INSERT INTO [dbo].[BuildDocuments] ([BuildId],[DocumentId]) VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperEventIdVar, StringConstants.SqlHelperDocIdVar);
                // sql command params for inserting currrent documnet
                cmd.Parameters.AddWithValue(string.Format("FileName{0}", docIdx), doc.FileName);
                cmd.Parameters.AddWithValue(string.Format("Content{0}", docIdx), doc.Content);

                foreach (var item in from errorItem in ErrorItems where errorItem.ErrorListItem != null select errorItem.ErrorListItem)
                {
                    errIdx++;

                    // sql statement for inserting a current documnet error item
                    sql.AppendFormat(@"{0}INSERT INTO dbo.ErrorListItems([Column],[Line],[File],[Project],[Description])
VALUES (@EColumn{1}{2},@ELine{1}{2},@EFile{1}{2},@EProject{1}{2},@EDescription{1}{2})", Environment.NewLine, docIdx, errIdx);
                    // sql command params for inserting currrent documnet error item
                    cmd.Parameters.AddWithValue(string.Format("EColumn{0}{1}", docIdx, errIdx), item.Column);
                    cmd.Parameters.AddWithValue(string.Format("ELine{0}{1}", docIdx, errIdx), item.Line);
                    cmd.Parameters.AddWithValue(string.Format("EFile{0}{1}", docIdx, errIdx), item.File);
                    cmd.Parameters.AddWithValue(string.Format("EProject{0}{1}", docIdx, errIdx), item.Project);
                    cmd.Parameters.AddWithValue(string.Format("EDescription{0}{1}", docIdx, errIdx), item.Description);

                    // sql statement for inserting linking table records
                    sql.AppendFormat(@"{0}SELECT {1}=SCOPE_IDENTITY()", Environment.NewLine, StringConstants.SqlHelperIdVar);
                    sql.AppendFormat(@"{0}INSERT INTO dbo.BuildEventErrorListItems([BuildEventId],[ErrorListItemId])
VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperEventIdVar, StringConstants.SqlHelperIdVar);
                    sql.AppendFormat(@"{0}INSERT INTO dbo.CodeDocumentErrorListItems([CodeFileId],[ErrorListItemId])
VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperDocIdVar, StringConstants.SqlHelperIdVar);
                }

                foreach (var item in from breakPoint in Breakpoints where breakPoint.BreakPoint != null select breakPoint.BreakPoint)
                {
                    bkIdx++;

                    // sql statement for inserting a current documnet breakpoint record
                    sql.AppendFormat(@"{0}INSERT INTO dbo.BreakPoints([Condition],[File],[FileColumn],[FileLine],[FunctionColumnOffset],[FunctionLineOffset],[FunctionName],[Name],[Enabled])
VALUES (@PCondition{1}{2}, @PFile{1}{2}, @PFileColumn{1}{2}, @PFileLine{1}{2}, @PFunctionColumnOffset{1}{2}, @PFunctionLineOffset{1}{2}, @PFunctionName{1}{2}, @PName{1}{2}, @PEnabled{1}{2})", Environment.NewLine, docIdx, bkIdx);
                    // sql command params for inserting currrent documnet breakpoint record
                    cmd.Parameters.AddWithValue(string.Format("PCondition{0}{1}", docIdx, bkIdx), item.Condition);
                    cmd.Parameters.AddWithValue(string.Format("PFile{0}{1}", docIdx, bkIdx), item.File);
                    cmd.Parameters.AddWithValue(string.Format("PFileColumn{0}{1}", docIdx, bkIdx), item.FileColumn);
                    cmd.Parameters.AddWithValue(string.Format("PFileLine{0}{1}", docIdx, bkIdx), item.FileLine);
                    cmd.Parameters.AddWithValue(string.Format("PFunctionColumnOffset{0}{1}", docIdx, bkIdx), item.FunctionColumnOffset);
                    cmd.Parameters.AddWithValue(string.Format("PFunctionLineOffset{0}{1}", docIdx, bkIdx), item.FunctionLineOffset);
                    cmd.Parameters.AddWithValue(string.Format("PFunctionName{0}{1}", docIdx, bkIdx), item.FunctionName);
                    cmd.Parameters.AddWithValue(string.Format("PName{0}{1}", docIdx, bkIdx), item.Name);
                    cmd.Parameters.AddWithValue(string.Format("PEnabled{0}{1}", docIdx, bkIdx), item.Enabled);

                    // sql statement for inserting linking table records
                    sql.AppendFormat(@"{0}SELECT {1}=SCOPE_IDENTITY()", Environment.NewLine, StringConstants.SqlHelperIdVar);
                    sql.AppendFormat(@"{0}INSERT INTO [dbo].[BuildEventBreakPoints] ([BuildEventId],[BreakPointId])
VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperEventIdVar, StringConstants.SqlHelperIdVar);
                    sql.AppendFormat(@"{0}INSERT INTO dbo.CodeDocumentBreakPoints([CodeFileId],[BreakPointId])
VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperDocIdVar, StringConstants.SqlHelperIdVar);
                }
            }
            // the final select statement to return the log Id as the scalar result
            sql.AppendFormat("{0}SELECT {1}", Environment.NewLine, StringConstants.SqlHelperLogIdVar);

            cmd.CommandText = sql.ToString();

            return cmd;
        }
    }
}
