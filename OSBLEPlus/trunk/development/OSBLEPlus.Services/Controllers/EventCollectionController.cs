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

            //to be compliant with vs client which only has auth token, web client may carry both auth token and sender id
            var logs = EventCollectionControllerHelper.GetActivityEvents(request);
            logs.ForEach(x => x.SenderId = auth.GetActiveUserId(request.AuthToken));
            var result = Posts.Post(logs);
            return new HttpResponseMessage
            {
                StatusCode = result > 0 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                Content = new StringContent(result.ToString())
            };
        }
    }
}