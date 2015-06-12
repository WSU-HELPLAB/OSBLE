namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class HelpfulMarkGivenEvent: ActivityEvent
    {
        public int LogCommentEventId { get; set; }
        public LogCommentEvent LogComment { get; set; }
        public HelpfulMarkGivenEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
