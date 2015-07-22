using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class CourseController : ApiController
    {
        [HttpGet]
        public DateTime MostRecentWhatsNewItem()
        {
            var recentWhatsNew = CourseDataAccess.GetMostRecentWhatsNewItem();
            return recentWhatsNew == null ? DateTime.MinValue : recentWhatsNew.DatePosted;
        }

        public List<SubmisionAssignment> GetAssignmentsForCourse(int id, string a)
        {
            if ((new Authentication()).IsValidKey(a))
            {
                return CourseDataAccess.GetAssignmentsForCourse(id, DateTime.Today);
            }

            return null;
        }

        public DateTime? GetLastSubmitDateForAssignment(int id, string a)
        {
            var auth = new Authentication();
            if (auth.IsValidKey(a))
            {
                return CourseDataAccess.GetLastSubmitDateForAssignment(id, (auth.GetActiveUserId(a)));
            }

            return null;
        }

        [HttpPost]
        public HttpResponseMessage Post([ModelBinder]SubmissionRequest request)
        {
            var auth = new Authentication();
            if (!auth.IsValidKey(request.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            if (request.SubmitEvent.Sender == null)
            {
                var sender = auth.GetActiveUser(request.AuthToken);
                request.SubmitEvent.SenderId = sender.UserId;
                request.SubmitEvent.Sender = new User
                {
                    UserId = sender.UserId,
                    FirstName = sender.FirstName,
                    LastName = sender.LastName
                };
            }

            string content = Posts.SubmitAssignment(request.SubmitEvent).ToString();
            Posts.SaveToFileSystem(request.SubmitEvent);

            return new HttpResponseMessage
            {
                Content = new StringContent(content)
            };
        }
    }
}