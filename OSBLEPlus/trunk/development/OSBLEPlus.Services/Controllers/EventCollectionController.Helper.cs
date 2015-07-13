using System.Collections.Generic;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;

namespace OSBLEPlus.Services.Controllers
{
    public class EventCollectionControllerHelper
    {
        public static List<IActivityEvent> GetActivityEvents(EventPostRequest requestObject)
        {
            var events = new List<IActivityEvent>();

            if (requestObject.AskHelpEvents != null && requestObject.AskHelpEvents.Length > 0)
                events.AddRange(requestObject.AskHelpEvents);

            if (requestObject.BuildEvents != null && requestObject.BuildEvents.Length > 0)
                events.AddRange(requestObject.BuildEvents);

            if (requestObject.CutCopyPasteEvents != null && requestObject.CutCopyPasteEvents.Length > 0)
                events.AddRange(requestObject.CutCopyPasteEvents);

            if (requestObject.DebugEvents != null && requestObject.DebugEvents.Length > 0)
                events.AddRange(requestObject.DebugEvents);

            if (requestObject.EditorActivityEvents != null && requestObject.EditorActivityEvents.Length > 0)
                events.AddRange(requestObject.EditorActivityEvents);

            if (requestObject.ExceptionEvents != null && requestObject.ExceptionEvents.Length > 0)
                events.AddRange(requestObject.ExceptionEvents);

            if (requestObject.FeedPostEvents != null && requestObject.FeedPostEvents.Length > 0)
                events.AddRange(requestObject.FeedPostEvents);

            if (requestObject.HelpfulMarkEvents != null && requestObject.HelpfulMarkEvents.Length > 0)
                events.AddRange(requestObject.HelpfulMarkEvents);

            if (requestObject.LogCommentEvents != null && requestObject.LogCommentEvents.Length > 0)
                events.AddRange(requestObject.LogCommentEvents);

            if (requestObject.SubmitEvents != null && requestObject.SubmitEvents.Length > 0)
                events.AddRange(requestObject.SubmitEvents);

            if (requestObject.SaveEvents != null && requestObject.SaveEvents.Length > 0)
                events.AddRange(requestObject.SaveEvents);

            return events;
        }
    }
}