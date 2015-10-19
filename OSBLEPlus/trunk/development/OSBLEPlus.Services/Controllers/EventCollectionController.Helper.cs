using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Services.Controllers
{
    public class EventCollectionControllerHelper
    {
        /// <summary>
        /// Finds the first non-null Event in an EventPostRequest and returns that
        /// </summary>
        /// <param name="requestObject">
        /// requestObject has the following POCO format
        /// public AskForHelpEvent AskHelpEvent { get; set; }
        /// public BuildEvent BuildEvent { get; set; }
        /// public CutCopyPasteEvent CutCopyPasteEvent { get; set; }
        /// public DebugEvent DebugEvent { get; set; }
        /// public EditorActivityEvent EditorActivityEvent { get; set; }
        /// public ExceptionEvent ExceptionEvent { get; set; }
        /// public FeedPostEvent FeedPostEvent { get; set; }
        /// public HelpfulMarkGivenEvent HelpfulMarkEvent { get; set; }
        /// public LogCommentEvent LogCommentEvent { get; set; }
        /// public SaveEvent SaveEvent { get; set; }
        /// public SubmitEvent SubmitEvent { get; set; }
        /// </param>
        /// <returns></returns>
        public static ActivityEvent GetActivityEvent(EventPostRequest requestObject)
        {
            if (requestObject.AskHelpEvent != null)
                return requestObject.AskHelpEvent;

            if (requestObject.BuildEvent != null)
                return requestObject.BuildEvent;

            if (requestObject.CutCopyPasteEvent != null)
                return requestObject.CutCopyPasteEvent;

            if (requestObject.DebugEvent != null)
                return requestObject.DebugEvent;

            if (requestObject.EditorActivityEvent != null)
                return requestObject.EditorActivityEvent;

            if (requestObject.ExceptionEvent != null)
                return requestObject.ExceptionEvent;

            if (requestObject.FeedPostEvent != null)
                return requestObject.FeedPostEvent;

            if (requestObject.HelpfulMarkEvent != null)
                return requestObject.HelpfulMarkEvent;

            if (requestObject.LogCommentEvent != null)
                return requestObject.LogCommentEvent;

            if (requestObject.SubmitEvent != null)
                return requestObject.SubmitEvent;

            return requestObject.SaveEvent;
        }
    }
}