using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Newtonsoft.Json;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interfaces;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class EventCollectionController : ApiController
    {
        public string Echo(string toEcho)
        {
            return toEcho;
        }

        public string Login(string e, string hp, IAuthentication auth = null)
        {
            var hash = string.Empty;
            if (UserDataAccess.ValidateUser(e, hp))
            {
                var user = UserDataAccess.GetByName(e);
                if (user != null)
                {
                    if (auth == null)
                        auth = new Authentication();

                    auth.LogIn(user);
                    UserDataAccess.LogUserTransaction(user.UserId, DateTime.Now);
                }
            }
            return hash;
        }

        public IUser GetActiveUser(string a, IAuthentication auth = null)
        {
            if (auth == null)
                auth = new Authentication();

            return auth.GetActiveUser(a);
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