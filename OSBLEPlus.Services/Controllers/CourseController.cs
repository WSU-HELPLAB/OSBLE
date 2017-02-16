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

            bool eventSenderNotNull = false;

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
                eventSenderNotNull = true;
            }

            var content = Posts.SaveEvent(request.SubmitEvent).ToString();
            Posts.SaveToFileSystem(request.SubmitEvent);

            try //don't want hub notification to break post
            {
                //push the event to the hub
                if (eventSenderNotNull)
                {
                    using (EventCollectionController ecc = new EventCollectionController())
                    {
                        ecc.NotifyHub(int.Parse(content), request.SubmitEvent.SenderId, request.SubmitEvent.EventType.ToString(), request.SubmitEvent.CourseId ?? 0);
                    }
                }
            }
            catch (Exception)
            {
                //ignore for now
            }

            //submit to intervetion controller
            ProcessLogForIntervention(request);

            return new HttpResponseMessage
            {
                Content = new StringContent(content)
            };
        }

        private async void ProcessLogForIntervention(SubmissionRequest request)
        {
            using (InterventionController intervention = new InterventionController())
            {
                intervention.ProcessActivityEvent(
                    EventCollectionControllerHelper.GetActivityEvent(
                        new EventPostRequest
                        {
                            AuthToken = request.AuthToken,
                            SubmitEvent = request.SubmitEvent
                        }
                    ));
            }
        }
    }
}