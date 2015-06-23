using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;

using Newtonsoft.Json;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class CourseController : ApiController
    {
        public List<SubmisionAssignment> GetAssignmentsForCourse(int id, string a, IAuthentication auth = null)
        {
            if (auth == null)
                auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                return CourseDataAccess.GetAssignmentsForCourse(id, DateTime.Today);
            }

            return null;
        }

        public DateTime? GetLastSubmitDateForAssignment(int id, string a, IAuthentication auth = null)
        {
            if (auth == null)
                auth = new Authentication();

            if (auth.IsValidKey(a))
            {
                return CourseDataAccess.GetLastSubmitDateForAssignment(id);
            }

            return null;
        }

        [HttpPost]
        public HttpResponseMessage Post(HttpRequestMessage request, IAuthentication auth = null)
        {
            var requestObject = JsonConvert.DeserializeObject<SubmissionRequest>(request.Content.ReadAsStringAsync().Result);

            if (auth == null)
                auth = new Authentication();

            if (!auth.IsValidKey(requestObject.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            return new HttpResponseMessage
            {
                Content = new StringContent(Posts.SubmitAssignment(requestObject.SubmitEvent).ToString())
            };
        }
    }
}