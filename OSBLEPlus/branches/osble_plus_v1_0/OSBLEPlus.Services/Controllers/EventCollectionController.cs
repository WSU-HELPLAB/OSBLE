using System.Net;
using System.Net.Http;
using System.Web.Http;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class EventCollectionController : ApiController
    {
        /// <summary>
        /// Echos back the sent string.
        /// </summary>
        /// <param name="toEcho">string to return</param>
        /// <returns>returns toEcho</returns>
        [HttpGet]
        public string Echo(string toEcho)
        {
            return toEcho;
        }

        /// <summary>
        /// Posts any Event sent via the EventPostRequest type
        /// </summary>
        /// <param name="request">
        /// request has the following POCO format
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
        [HttpPost]
        public HttpResponseMessage Post(EventPostRequest request)
        {
            var auth = new Authentication();
            if (!auth.IsValidKey(request.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            var log = EventCollectionControllerHelper.GetActivityEvent(request);
            log.SenderId = auth.GetActiveUserId(request.AuthToken);
            var result = Posts.SaveEvent(log);

            return new HttpResponseMessage
            {
                StatusCode = result > 0 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK,
                Content = new StringContent(result.ToString())
            };
        }
    }
}