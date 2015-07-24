﻿using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public sealed class AskForHelpEvent : ActivityEvent
    {
        public string Code { get; set; }
        public string UserComment { get; set; }

        public AskForHelpEvent() // NOTE!! This is required by Dapper ORM
        {
            EventTypeId = (int)Utility.Lookups.EventType.AskForHelpEvent;
        }

        public AskForHelpEvent(DateTime dateTimeValue)
            : this()
        {
            EventDate = dateTimeValue;
        }

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeId, EventDate, SenderId, BatchId, CourseId) VALUES ({0}, '{1}', {2}, '{6}', {7})
INSERT INTO dbo.AskForHelpEvents (EventLogId, EventDate, SolutionName, Code, UserComment)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}', '{4}', '{5}')", EventTypeId, EventDate, SenderId, SolutionName, Code, UserComment, BatchId, CourseId);
        }
    }
}
