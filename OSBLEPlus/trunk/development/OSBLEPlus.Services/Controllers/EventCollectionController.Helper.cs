using System.Collections.Generic;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interfaces;

namespace OSBLEPlus.Services.Controllers
{
    public class EventCollectionControllerHelper
    {
        public static IEnumerable<IActivityEvent> GetActivityEvents(EventPostRequest requestObject)
        {
            var events = new List<IActivityEvent>();

            if (requestObject.AskHelpEvents != null && requestObject.AskHelpEvents.Length > 0)
                events.AddRange(requestObject.AskHelpEvents);

            if (requestObject.BuildEvents != null && requestObject.BuildEvents.Length > 0)
                events.AddRange(requestObject.BuildEvents);

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

            return events;
        }
    }
}