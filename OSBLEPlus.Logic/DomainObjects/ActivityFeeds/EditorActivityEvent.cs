﻿using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public sealed class EditorActivityEvent : ActivityEvent
    {
        public EditorActivityEvent()
        {
            EventTypeId = (int)EventType.EditorActivityEvent;
        }

        public override string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO dbo.EventLogs (EventTypeID, EventDate, SenderId, BatchId) VALUES ({0}, '{1}', {2}, {4})
INSERT INTO dbo.EditorActivityEvents (EventLogId, EventDate, SolutionName)
VALUES (SCOPE_IDENTITY(), '{1}', '{3}')", EventTypeId, EventDate, SenderId, SolutionName, BatchId);
        }
    }
}
