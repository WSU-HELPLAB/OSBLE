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
        [HttpGet]
        public string Echo(string toEcho)
        {
            return toEcho;
        }

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