using System;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class BuildEventErrorListItem
    {
        public int BuildEventId { get; set; }

        public virtual BuildEvent BuildEvent { get; set; }

        public int ErrorListItemId { get; set; }

        public virtual ErrorListItem ErrorListItem { get; set; }
    }
}
