namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class EventPostRequest
    {
        public string AuthToken { get; set; }
        public AskForHelpEvent[] AskHelpEvents { get; set; }
        public BuildEvent[] BuildEvents { get; set; }
        public ExceptionEvent[] ExceptionEvents { get; set; }
        public FeedPostEvent[] FeedPostEvents { get; set; }
        public HelpfulMarkGivenEvent[] HelpfulMarkEvents { get; set; }
        public LogCommentEvent[] LogCommentEvents { get; set; }
        public SubmitEvent[] SubmitEvents { get; set; }
    }
}
