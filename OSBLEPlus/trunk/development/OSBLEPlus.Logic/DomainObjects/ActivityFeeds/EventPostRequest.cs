namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class EventPostRequest
    {
        public string AuthToken { get; set; }

        /// <summary>
        /// this is efficient for xml serialization
        /// </summary>
        public AskForHelpEvent AskHelpEvent { get; set; }
        public BuildEvent BuildEvent { get; set; }
        public CutCopyPasteEvent CutCopyPasteEvent { get; set; }
        public DebugEvent DebugEvent { get; set; }
        public EditorActivityEvent EditorActivityEvent { get; set; }
        public ExceptionEvent ExceptionEvent { get; set; }
        public FeedPostEvent FeedPostEvent { get; set; }
        public HelpfulMarkGivenEvent HelpfulMarkEvent { get; set; }
        public LogCommentEvent LogCommentEvent { get; set; }
        public SaveEvent SaveEvent { get; set; }
        public SubmitEvent SubmitEvent { get; set; }
    }

    public class SubmissionRequest
    {
        public string AuthToken { get; set; }
        public SubmitEvent SubmitEvent { get; set; }
    }
}
