using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Controllers
{
    public class CourseController : ApiController
    {
        /// <summary>
        /// Returns the DateTime of the most recent item
        /// </summary>
        /// <returns>DateTime</returns>
        [HttpGet]
        public DateTime MostRecentWhatsNewItem()
        {
            var recentWhatsNew = CourseDataAccess.GetMostRecentWhatsNewItem();
            return recentWhatsNew == null ? DateTime.MinValue : recentWhatsNew.DatePosted;
        }

        /// <summary>
        /// Returns a list of Submission assignments
        /// </summary>
        /// <param name="id">Course ID for the class</param>
        /// <param name="a">Authentication Key for the Course to access the assignment</param>
        /// <returns></returns>
        public List<SubmisionAssignment> GetAssignmentsForCourse(int id, string a)
        {
            if ((new Authentication()).IsValidKey(a))
            {
                return CourseDataAccess.GetAssignmentsForCourse(id, DateTime.UtcNow);
            }

            return null;
        }

        /// <summary>
        /// Get's the last submit date for an assignment
        /// </summary>
        /// <param name="id">Course ID for the class</param>
        /// <param name="a">Authentication Key for the Course to access the assignment</param>
        /// <returns></returns>
        public DateTime? GetLastSubmitDateForAssignment(int id, string a)
        {
            var auth = new Authentication();
            if (auth.IsValidKey(a))
            {
                return CourseDataAccess.GetLastSubmitDateForAssignment(id, (auth.GetActiveUserId(a)));
            }

            return null;
        }

        /// <summary>
        /// Post submits and assignment
        /// </summary>
        /// <param name="request">
        /// request has the following POCO Format:
        /// public string AuthToken { get; set; }
        /// public SubmitEvent SubmitEvent { get; set; }
        /// </param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post(SubmissionRequest request)
        {
            var auth = new Authentication();
            if (!auth.IsValidKey(request.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            if (request.SubmitEvent.Sender == null)
            {
                var sender = auth.GetActiveUser(request.AuthToken);
                request.SubmitEvent.SenderId = sender.IUserId;
                request.SubmitEvent.Sender = new User
                {
                    IUserId = sender.IUserId,
                    FirstName = sender.FirstName,
                    LastName = sender.LastName
                };
            }

            var content = Posts.SaveEvent(request.SubmitEvent).ToString();
            Posts.SaveToFileSystem(request.SubmitEvent);

            return new HttpResponseMessage
            {
                Content = new StringContent(content)
            };
        }
    }
}