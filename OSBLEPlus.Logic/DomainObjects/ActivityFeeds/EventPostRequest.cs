using OSBLEPlus.Logic.Utility;
namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class EventPostRequest
    {
        public string AuthToken { get; set; }
        public AskForHelpEvent[] AskHelpEvents { get; set; }
        public BuildEvent[] BuildEvents { get; set; }
        public CutCopyPasteEvent[] CutCopyPasteEvents { get; set; }
        public DebugEvent[] DebugEvents { get; set; }
        public EditorActivityEvent[] EditorActivityEvents { get; set; }
        public ExceptionEvent[] ExceptionEvents { get; set; }
        public FeedPostEvent[] FeedPostEvents { get; set; }
        public HelpfulMarkGivenEvent[] HelpfulMarkEvents { get; set; }
        public LogCommentEvent[] LogCommentEvents { get; set; }
        public SaveEvent[] SaveEvents { get; set; }
        public SubmitEvent[] SubmitEvents { get; set; }
    }

    public class SubmissionRequest
    {
        public string AuthToken { get; set; }
        public SubmitEvent SubmitEvent { get; set; }

        public int TeamId { get; set; }

        public string RequestData { get; set; }
    }
}
