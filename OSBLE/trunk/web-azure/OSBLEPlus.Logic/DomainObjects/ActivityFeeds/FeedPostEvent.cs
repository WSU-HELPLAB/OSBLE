using System;
namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class FeedPostEvent: ActivityEvent
    {
        public string Comment { get; set; }
        public FeedPostEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
