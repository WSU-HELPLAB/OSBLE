using System.Net;
using System.Net.Http;
using System.Web.Http;

using Newtonsoft.Json;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class EventCollectionController : ApiController
    {
        public string Echo(string toEcho)
        {
            return toEcho;
        }

        [HttpPost]
        public HttpResponseMessage Post(HttpRequestMessage request, IAuthentication auth = null)
        {
            var requestObject = JsonConvert.DeserializeObject<EventPostRequest>(request.Content.ReadAsStringAsync().Result);

            if (auth == null)
                auth = new Authentication();

            if (!auth.IsValidKey(requestObject.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            return new HttpResponseMessage
            {
                Content = new StringContent(Posts.Post(EventCollectionControllerHelper.GetActivityEvents(requestObject)).ToString()),
            };
        }
    }
}