using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using OSBLE.Interfaces;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.Helpers;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class UserProfilesController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Login(string e, string hp)
        {
            if (!UserDataAccess.ValidateUser(e, hp))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized };

            var user = UserDataAccess.GetByName(e);
            if (user == null)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized };

            var auth = new Authentication();
            var hash = auth.LogIn(user);
            UserDataAccess.LogUserTransaction(user.UserId, DateTime.Now);

            return new HttpResponseMessage
            {
                Content = new StringContent(hash),
                StatusCode = HttpStatusCode.OK
            };
        }

        public IUser GetActiveUser(string a)
        {
            return (new Authentication()).GetActiveUser(a);
        }

        [HttpGet]
        public bool IsValidKey(string a)
        {
            var auth = new Authentication();
            if (auth.IsValidKey(a))
            {
                UserDataAccess.LogUserTransaction(auth.GetActiveUserId(a), DateTime.Now);
                return true;
            }

            return false;
        }

        public List<ProfileCourse> GetCoursesForUser(string a)
        {
            var auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                return UserDataAccess.GetProfileCoursesForUser(auth.GetActiveUserId(a), DateTime.Today);
            }

            return null;
        }

        [HttpGet]
        public DateTime MostRecentSocialActivity(string a)
        {
            var lastSocialActivity = DateTime.MinValue;

            var auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                return UserDataAccess.GetMostRecentSocialActivityForUser(auth.GetActiveUserId(a));
            }

            return lastSocialActivity;
        }

        [HttpPost]
        public HttpResponseMessage SubmitLocalErrorLog(LocalErrorLogRequest request)
        {
            var auth = new Authentication();

            if (!auth.IsValidKey(request.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            request.Log.SenderId = auth.GetActiveUserId(request.AuthToken);

            return new HttpResponseMessage
            {
                StatusCode = Posts.SubmitLocalErrorLog(request.Log) > 0
                           ? HttpStatusCode.OK
                           : HttpStatusCode.InternalServerError
            };
        }
    }
}