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

            //post to feed hub here.
            NotifyHub(result, log.SenderId, log.EventType.ToString(), log.CourseId ?? 0);

            return new HttpResponseMessage
            {
                StatusCode = result > 0 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                Content = new StringContent(result.ToString())
            };
        }

        public void NotifyHub(int logId, int userId, string eventType, int courseId = 0)
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

                    var result =  sqlConnection.Query(query, new { senderId = userId }).AsList();

                    courseId = result[0].CourseId;

                    sqlConnection.Close();
                }
            }

            var connection = new HubConnection(StringConstants.WebClientRoot, "userID=" + userId + "&courseID=" + courseId + "&logID=" + logId + "&eventType=" + eventType, true);
            connection.Headers.Add("Host", (new Uri(StringConstants.WebClientRoot)).Host);
            connection.Headers.Add("Origin", StringConstants.WebClientRoot);

            IHubProxy hub = connection.CreateHubProxy("ActivityFeedHub");
            
            connection.Start().Wait();

            hub.Invoke("ForwardPluginEventToFeed");

            connection.Stop();
        }
    }
}