using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using OSBLE.Utility;
using Newtonsoft.Json.Linq;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.Utility.Auth;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLE.Controllers;
using Newtonsoft.Json;

namespace OSBLE.Hubs
{
    public class ActivityFeedHub : Hub
    {
        public void JoinCourse()
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            int userID = int.Parse(Context.QueryString["userID"]);

            // verify user has access to the course
            var cu = DBHelper.GetCourseUserFromProfileAndCourse(userID, courseID);
            if (cu == null)
                return;

            // add to course group
            Groups.Add(Context.ConnectionId, courseID.ToString());
        }

        public void LeaveCourse()
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Groups.Remove(Context.ConnectionId, courseID.ToString());
        }

        public override System.Threading.Tasks.Task OnConnected()
        {
            JoinCourse();
            return base.OnConnected();
        }
        public void NotifyNewSuggestion()
        {
            int courseId = int.Parse(Context.QueryString["courseID"]);
            int userProfileId = int.Parse(Context.QueryString["userID"]);
            string authKey = Context.QueryString["authKey"];

            // verify user has access to the course
            var cu = DBHelper.GetCourseUserFromProfileAndCourse(userProfileId, courseId);
            if (cu == null)
                return;

            Clients.Group(courseId.ToString()).notifyNewSuggestion(userProfileId);
        }

        public void NotifyNewReply(int postID, object replyList)
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);

            Clients.Group(courseID.ToString()).addNewReply(postID, replyList);
        }

        public void NotifyNewPost(object post, string eventType = "", int courseID = 0, string authKey = "")
        {
            //TODO: code for exception events
            //if (eventType == "ExceptionEvent") //plugin event, skip validation for now?
            //{
            //    List<int> activeCourses = DBHelper.GetActiveCourseIds();
            //    foreach (int id in activeCourses)
            //    {
            //        Clients.Group(id.ToString()).addNewPost(id, post);
            //    }
            //    return;
            //}           

            //verify that the post is legit before pushing out to listeners
            //will match the post information with the cookie authkey and the database event.
            JObject feedPost = JObject.FromObject(post);
            int eventLogId = (int)feedPost.SelectToken("EventId");
            courseID = int.Parse(Context.QueryString["courseID"]);
            int senderId = (int)feedPost.SelectToken("SenderId");
            string senderFullName = (string)feedPost.SelectToken("SenderName");

            if (eventType == "AskForHelpEvent") //plugin event, skip validation for now?
            {
                //push the message to all active courses the user is involved in
                //do this because (currently) ask for help events do not get a courseId when saved.
                List<int> activeCourses = DBHelper.GetActiveCourseIds(senderId);
                foreach (int id in activeCourses)
                {
                    Clients.Group(id.ToString()).addNewPost(id, post);
                }
                return;
            }

            //need courseId from the above for the next two event types
            if (eventType == "SubmitEvent") //plugin event, skip validation for now?
            {
                //arbitrarily choose 5 minutes
                //if they have submitted at least 5 minutes ago, go ahead and push the notification to the feed
                //otherwise, don't push out the notification (to avoid submit spamming the feed)
                if (DBHelper.LastSubmitGreaterThanMinutesInterval(eventLogId, 5))
                {
                    Clients.Group(courseID.ToString()).addNewPost(courseID, post);
                }
                return;
            }

            try
            {
                if (authKey == "")
                {
                    authKey = Context.Request.Cookies["AuthKey"].Value.Split('=').Last();
                }
            }
            catch (Exception)
            {
                return; //no authkey cookie or split error so it will not authenticate
            }

            Authentication auth = new Authentication();
            bool validAuth = auth.IsValidKey(authKey);

            if (!validAuth)
                return; //will not validate, just exit

            UserProfile authUserProfile = auth.GetActiveUser(authKey);

            //verify to this post and user data from the database
            ActivityEvent verifyPost = DBHelper.GetActivityEvent(eventLogId);
            UserProfile profile = DBHelper.GetUserProfile(verifyPost.SenderId);

            if (validAuth && //their cookie authkey has to be valid
                new[] { verifyPost.SenderId, senderId, authUserProfile.ID }.All(sid => sid == verifyPost.SenderId) && //the sender Id has to match
                verifyPost.CourseId == courseID && //the course has to match
                (new[] { profile.FullName, senderFullName, authUserProfile.FullName }.All(pfn => pfn == profile.FullName)) || senderFullName.Contains("Anonymous")) //the user full name has to match
            {
                Clients.Group(courseID.ToString()).addNewPost(courseID, post);
            }
        }

        public void NotifyEditPost(int postID, string newContent, string timeString)
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Clients.Group(courseID.ToString()).editPost(postID, newContent, timeString);
        }

        public void NotifyEditReply(int postID, int replyID, string newContent, string timeString)
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Clients.Group(courseID.ToString()).editReply(postID, replyID, newContent);
        }

        // Note: this method can also remove helpful marks
        public void NotifyAddMarkHelpful(int parentPostID, int replyPostID, int numHelpfulMarks)
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Clients.Group(courseID.ToString()).addMarkHelpful(parentPostID, replyPostID, numHelpfulMarks);
        }

        public void ForwardPluginEventToFeed()
        {
            int courseId = int.Parse(Context.QueryString["courseID"]);
            int logId = int.Parse(Context.QueryString["logID"]);
            int userId = int.Parse(Context.QueryString["userID"]);
            string eventType = Context.QueryString["eventType"];
            string authKey = Context.QueryString["authKey"];            

            var newPost = new AggregateFeedItem(Feeds.Get(logId));
            using (FeedController feedController = new FeedController())
            {
                NotifyNewPost(JObject.Parse(JsonConvert.SerializeObject(feedController.MakeAggregateFeedItemJsonObject(newPost, false, userId, courseId))), eventType, courseId, authKey);
            }            
        }
    }
}