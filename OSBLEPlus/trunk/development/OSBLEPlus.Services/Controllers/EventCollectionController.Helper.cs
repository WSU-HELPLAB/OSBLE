using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Services.Controllers
{
    public class EventCollectionControllerHelper
    {
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