using System.Collections.Generic;
using OSBLEPlus.Logic.DomainObjects.Interface;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class FeedItem
    {
        public IActivityEvent Event { get; set; }

        public List<LogCommentEvent> Comments { get; set; }
    }
}