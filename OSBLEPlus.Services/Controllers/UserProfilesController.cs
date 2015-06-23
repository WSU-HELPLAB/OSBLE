using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Newtonsoft.Json;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.Helpers;
using OSBLEPlus.Logic.DomainObjects.Interfaces;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class UserProfilesController : ApiController
    {
        public HttpResponseMessage Login(string e, string hp, IAuthentication auth = null)
        {
            if (!UserDataAccess.ValidateUser(e, hp))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized };

            var user = UserDataAccess.GetByName(e);
            if (user == null)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized };

            if (auth == null)
                auth = new Authentication();

            var hash = auth.LogIn(user);
            UserDataAccess.LogUserTransaction(user.UserId, DateTime.Now);

            return new HttpResponseMessage
            {
                Content = new StringContent(hash),
                StatusCode = HttpStatusCode.OK
            };
        }

        public IUser GetActiveUser(string a, IAuthentication auth = null)
        {
            if (auth == null)
                auth = new Authentication();

            return auth.GetActiveUser(a);
        }

        public bool IsValidKey(string a, IAuthentication auth = null)
        {
            if (auth == null)
                auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                UserDataAccess.LogUserTransaction(auth.GetActiveUserId(a), DateTime.Now);
                return true;
            }

            return false;
        }

        public List<ProfileCourse> GetCoursesForUser(string a, IAuthentication auth = null)
        {
            if (auth == null)
                auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                return UserDataAccess.GetProfileCoursesForUser(auth.GetActiveUserId(a), DateTime.Today);
            }

            return null;
        }

        public DateTime MostRecentSocialActivity(string a, IAuthentication auth = null)
        {
            var lastSocialActivity = DateTime.MinValue;

            if (auth == null)
                auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                return UserDataAccess.GetMostRecentSocialActivityForUser(auth.GetActiveUserId(a));
            }

            return lastSocialActivity;
        }

        [HttpPost]
        public HttpResponseMessage SubmitLocalErrorLog(HttpRequestMessage request, IAuthentication auth = null)
        {
            var requestObject = JsonConvert.DeserializeObject<LocalErrorLogRequest>(request.Content.ReadAsStringAsync().Result);
            if (auth == null)
                auth = new Authentication();

            if (!auth.IsValidKey(requestObject.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            requestObject.Log.SenderId = auth.GetActiveUserId(requestObject.AuthToken);

            return new HttpResponseMessage
            {
                StatusCode = Posts.SubmitLocalErrorLog(requestObject.Log) == 0
                           ? HttpStatusCode.OK
                           : HttpStatusCode.InternalServerError
            };
        }
    }
}