﻿using System;
using System.Collections.Generic;

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

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {12})
INSERT INTO dbo.ExceptionEvents (EventLogId,EventDate,SolutionName,ExceptionType,ExceptionName,ExceptionCode,ExceptionDescription,ExceptionAction,DocumentName,LineNumber,LineContent)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}')",
            EventTypeId, EventDate, SenderId, SolutionName,
            ExceptionType, ExceptionName, ExceptionCode, ExceptionDescription, ExceptionAction,
            DocumentName, LineNumber, LineContent, BatchId);
        }
    }
}