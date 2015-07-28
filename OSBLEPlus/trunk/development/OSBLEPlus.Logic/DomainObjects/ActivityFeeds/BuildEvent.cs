using System;
using System.Collections.Generic;
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

        
        public List<BuildDocument> Documents {
            get
            {
                return _privateBuildDocuments;
            } 
            set
            {
                _privateBuildDocuments = value;
            } 
        }

        [NonSerialized]
        private List<BuildDocument> _privateBuildDocuments;

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

        public override string GetInsertScripts()
        {
            var sql = new StringBuilder(string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {4})
INSERT INTO dbo.BuildEvents (EventLogId, EventDate, SolutionName)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}')
SELECT {5}=SCOPE_IDENTITY()", EventTypeId, EventDate, SenderId, SolutionName, BatchId, StringConstants.SqlHelperEventIdVar));

            foreach (var doc in from document in Documents where document.Document != null select document.Document)
            {
                sql.AppendFormat(@"{0}INSERT INTO dbo.CodeDocuments([FileName],[Content]) VALUES ('{1}','{2}')", Environment.NewLine, doc.FileName, doc.Content);
                sql.AppendFormat(@"{0}SELECT {1}=SCOPE_IDENTITY()", Environment.NewLine, StringConstants.SqlHelperDocIdVar);
                sql.AppendFormat(@"{0}INSERT INTO [dbo].[BuildDocuments] ([BuildEventId],[DocumentId]) VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperEventIdVar, StringConstants.SqlHelperDocIdVar);

                foreach (var item in from errorItem in ErrorItems where errorItem.ErrorListItem != null select errorItem.ErrorListItem)
                {
                    sql.AppendFormat(@"{0}INSERT INTO dbo.ErrorListItems([Column],[Line],[File],[Project],[Description])
VALUES ({1},{2},'{3}','{4}','{5}')", Environment.NewLine, item.Column, item.Line, item.File, item.Project, item.Description);

                    sql.AppendFormat(@"{0}SELECT {1}=SCOPE_IDENTITY()", Environment.NewLine, StringConstants.SqlHelperIdVar);
                    sql.AppendFormat(@"{0}INSERT INTO dbo.BuildEventErrorListItems([BuildEventId],[ErrorListItemId])
VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperEventIdVar, StringConstants.SqlHelperIdVar);
                    sql.AppendFormat(@"{0}INSERT INTO dbo.CodeDocumentErrorListItems([CodeFileId],[ErrorListItemId])
VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperDocIdVar, StringConstants.SqlHelperIdVar);
                }

                foreach (var item in from breakPoint in Breakpoints where breakPoint.BreakPoint != null select breakPoint.BreakPoint)
                {
                    sql.AppendFormat(@"{0}INSERT INTO dbo.BreakPoints([Condition],[File],[FileColumn],[FileLine],[FunctionColumnOffset],[FunctionLineOffset],[FunctionName],[Name],[Enabled])
VALUES ('{1}','{2}',{3},{4},{5},{6},'{7}',{8})", Environment.NewLine, item.Condition, item.File, item.FileColumn, item.FileLine, item.FunctionColumnOffset, item.FunctionLineOffset, item.Name, item.Enabled ? 1 : 0);

                    sql.AppendFormat(@"{0}SELECT {1}=SCOPE_IDENTITY()", Environment.NewLine, StringConstants.SqlHelperIdVar);
                    sql.AppendFormat(@"{0}INSERT INTO [dbo].[BuildEventBreakPoints] ([BuildEventId],[BreakPointId])
VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperEventIdVar, StringConstants.SqlHelperIdVar);
                    sql.AppendFormat(@"{0}INSERT INTO dbo.CodeDocumentBreakPoints([CodeFileId],[BreakPointId])
VALUES ({1},{2})", Environment.NewLine, StringConstants.SqlHelperDocIdVar, StringConstants.SqlHelperIdVar);
                }
            }

            return sql.ToString();
        }
    }
}
