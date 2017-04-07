using System.Net;
using System.Net.Http;
using System.Web.Http;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility.Auth;

using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using OSBLEPlus.Logic.Utility;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Remoting.Contexts;
using System.Data.SqlClient;
using Dapper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OSBLEPlus.Services.Controllers
{
    public class EventCollectionController : ApiController
    {
        /// <summary>
        /// Echos back the sent string.
        /// </summary>
        /// <param name="toEcho">string to return</param>
        /// <returns>returns toEcho</returns>
        [HttpGet]
        public string Echo(string toEcho)
        {
            return toEcho;
        }

        /// <summary>
        /// Posts any Event sent via the EventPostRequest type
        /// </summary>
        /// <param name="request">
        /// request has the following POCO format
        /// public AskForHelpEvent AskHelpEvent { get; set; }
        /// public BuildEvent BuildEvent { get; set; }
        /// public CutCopyPasteEvent CutCopyPasteEvent { get; set; }
        /// public DebugEvent DebugEvent { get; set; }
        /// public EditorActivityEvent EditorActivityEvent { get; set; }
        /// public ExceptionEvent ExceptionEvent { get; set; }
        /// public FeedPostEvent FeedPostEvent { get; set; }
        /// public HelpfulMarkGivenEvent HelpfulMarkEvent { get; set; }
        /// public LogCommentEvent LogCommentEvent { get; set; }
        /// public SaveEvent SaveEvent { get; set; }
        /// public SubmitEvent SubmitEvent { get; set; }
        /// </param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage Post(EventPostRequest request)
        {
            var auth = new Authentication();
            if (!auth.IsValidKey(request.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

            var log = EventCollectionControllerHelper.GetActivityEvent(request);
            log.SenderId = auth.GetActiveUserId(request.AuthToken);

            var result = Posts.SaveEvent(log);

            //For now we're only pushing these events to the hub
            if (log.EventType.ToString() == "AskForHelpEvent" || log.EventType.ToString() == "SubmitEvent")
            {
                //post to feed hub here.
                NotifyHub(result, log.SenderId, log.EventType.ToString(), log.CourseId ?? 0);
            }

            //we've processed the log, now process for intervention.
            ProcessLogForIntervention(log);


            //push suggestion changes (it will only do so if suggestions need refreshing)
            NotifyNewSuggestion(log.SenderId, log.CourseId ?? 0, request.AuthToken);

            return new HttpResponseMessage
            {
                StatusCode = result > 0 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                Content = new StringContent(result.ToString())
            };
        }

        public void NotifyHub(int logId, int userId, string eventType, int courseId = 0, string authKey = "")
        {
            if (courseId == 0) //guess course with the most recent activity...
            {   //will need to do this for ask for help/exception events until a courseId is associated with them                
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    string query = "SELECT ISNULL( " +
                                    "(SELECT TOP 1 CourseId " +
                                    "FROM EventLogs " +
                                    "WHERE SenderId = @senderId " +
                                    "AND CourseId IS NOT NULL " +
                                    "ORDER BY EventDate DESC) " +
                                    ", 0) AS CourseId ";

                    var result = sqlConnection.Query(query, new { senderId = userId }).AsList();

                    courseId = result[0].CourseId;

                    sqlConnection.Close();
                }
            }

            var connection = new HubConnection(StringConstants.WebClientRoot, "userID=" + userId + "&courseID=" + courseId + "&logID=" + logId + "&eventType=" + eventType + "&authKey=" + authKey, true);
            connection.Headers.Add("Host", (new Uri(StringConstants.WebClientRoot)).Host);
            connection.Headers.Add("Origin", StringConstants.WebClientRoot);

            IHubProxy hub = connection.CreateHubProxy("ActivityFeedHub");

            connection.Start().Wait();

            hub.Invoke("ForwardPluginEventToFeed");

            //stop the connection after the message has been forwarded.            
            connection.Stop();
        }

        private async void ProcessLogForIntervention(ActivityEvent log)
        {
            using (InterventionController intervention = new InterventionController())
            {
                intervention.ProcessActivityEvent(log);
            }
        }

        public void NotifyNewSuggestion(int userId, int courseId = 0, string authKey = "")
        {
            bool refreshSuggestion = false;
            if (!String.IsNullOrEmpty(authKey))
            {
                InterventionController ic = new InterventionController();
                refreshSuggestion = ic.RefreshInterventionsOnDashboard(authKey);
            }

            if (refreshSuggestion)
            {
                List<int> activeCourseIds = new List<int>(); //push notification to all active courses that have suggestions enabled for this user
                if (courseId == 0) 
                {   
                    using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        sqlConnection.Open();
                        string query = "SELECT DISTINCT ac.ID FROM AbstractCourses ac " + 
                                       "INNER JOIN CourseUsers cu " + 
                                       "ON ac.ID = cu.AbstractCourseID " + 
                                       "INNER JOIN OSBLEInterventionsCourses oic " +
                                       "ON ac.ID = oic.CourseId " + 
                                       "WHERE cu.UserProfileID = @UserProfileId ";

                        var result = sqlConnection.Query(query, new { UserProfileId = userId });

                        if (result != null)
                        {
                            foreach (var item in result)
                            {
                                activeCourseIds.Add(item.ID);
                            }                            
                        }                        

                        sqlConnection.Close();
                    }
                }

                foreach (int id in activeCourseIds) //send to all 'active' courses with suggestions enabled
                {
                    var connection = new HubConnection(StringConstants.WebClientRoot, "userID=" + userId + "&courseID=" + id + "&authKey=" + authKey, true);
                    connection.Headers.Add("Host", (new Uri(StringConstants.WebClientRoot)).Host);
                    connection.Headers.Add("Origin", StringConstants.WebClientRoot);

                    IHubProxy hub = connection.CreateHubProxy("ActivityFeedHub");

                    connection.Start().Wait();

                    hub.Invoke("NotifyNewSuggestion");

                    //stop the connection after the message has been forwarded.            
                    connection.Stop();
                }                
            }
        }
    }
}