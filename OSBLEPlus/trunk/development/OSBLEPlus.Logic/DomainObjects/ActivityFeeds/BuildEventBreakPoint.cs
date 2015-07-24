﻿using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class BuildEventBreakPoint
    {
        public int BuildEventId { get; set; }

        public virtual BuildEvent BuildEvent { get; set; }

        public int BreakPointId { get; set; }

        public virtual BreakPoint BreakPoint { get; set; }
    }
}
