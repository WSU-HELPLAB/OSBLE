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
        private readonly Authentication _auth;

        public EventCollectionController() : this(new Authentication())
        {           
        }

        public EventCollectionController(Authentication auth)
        {
            _auth = auth;
        }

        public string Echo(string toEcho)
        {
            return toEcho;
        }

        public string Login(string email, string hashedPassword)
        {
            var hash = string.Empty;
            if (UserDataAccess.ValidateUser(email, hashedPassword))
            {
                var user = UserDataAccess.GetByName(email);
                if (user != null)
                {
                    _auth.LogIn(user);
                    UserDataAccess.LogUserTransaction(user.UserId, DateTime.Now);
                }
            }
            return hash;
        }

        public IUser GetActiveUser(string authToken)
        {
            return _auth.GetActiveUser(authToken);
        }

        public bool IsValidKey(string authToken)
        {
            var isValid = false;
            if (_auth.IsValidKey(authToken))
            {
                UserDataAccess.LogUserTransaction(_auth.GetActiveUserId(authToken), DateTime.Now);
                isValid = true;
            }
            return isValid;
        }

        [HttpPost]
        public HttpResponseMessage Post(HttpRequestMessage request)
        {
            var requestObject = JsonConvert.DeserializeObject<EventPostRequest>(request.Content.ReadAsStringAsync().Result);

            // testing request can go across the wire
            //if (requestObject.AuthToken == "test")
            //    return new HttpResponseMessage
            //    {
            //        StatusCode = EventCollectionControllerHelper.GetActivityEvents(requestObject).Any()
            //            ? HttpStatusCode.OK
            //            : HttpStatusCode.InternalServerError
            //    };

            if (!IsValidKey(requestObject.AuthToken))
                return new HttpResponseMessage {StatusCode = HttpStatusCode.Forbidden};

            return new HttpResponseMessage
            {
                StatusCode = Posts.Post(EventCollectionControllerHelper.GetActivityEvents(requestObject))
                           ? HttpStatusCode.OK
                           : HttpStatusCode.InternalServerError
            }; 
        }
    }
}