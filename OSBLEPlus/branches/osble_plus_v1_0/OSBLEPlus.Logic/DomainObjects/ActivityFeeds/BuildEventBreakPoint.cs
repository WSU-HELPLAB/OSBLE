using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class BuildEventBreakPoint
    {
        public int BuildEventId { get; set; }

        public virtual BuildEvent BuildEvent { get { return _build; } set { _build = value; } }

        [NonSerialized]
        private BuildEvent _build;

        public int BreakPointId { get; set; }

        public virtual BreakPoint BreakPoint { get; set; }
    }
}
