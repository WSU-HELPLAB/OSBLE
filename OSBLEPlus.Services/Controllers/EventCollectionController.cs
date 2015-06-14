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

        public string Login(string email, string hashedPassword)
        {
            var hash = string.Empty;
            if (UserDataAccess.ValidateUser(email, hashedPassword))
            {
                var user = UserDataAccess.GetByName(email);
                if (user != null)
                {
                    (new Authentication(System.IO.Path.GetDirectoryName(
      System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase))).LogIn(user);
                    UserDataAccess.LogUserTransaction(user.UserId, DateTime.Now);
                }
            }
            return hash;
        }

        public IUser GetActiveUser(string authToken)
        {
            return (new Authentication()).GetActiveUser(authToken);
        }

        public bool IsValidKey(string authToken)
        {
            var auth = new Authentication();
            var isValid = false;
            if (auth.IsValidKey(authToken))
            {
                UserDataAccess.LogUserTransaction(auth.GetActiveUserId(authToken), DateTime.Now);
                isValid = true;
            }
            return isValid;
        }

        [HttpPost]
        public HttpResponseMessage Post(HttpRequestMessage request)
        {
            var requestObject = JsonConvert.DeserializeObject<EventPostRequest>(request.Content.ReadAsStringAsync().Result);

            if (!(new Authentication()).IsValidKey(requestObject.AuthToken))
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