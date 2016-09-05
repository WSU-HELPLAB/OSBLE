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

        public void NotifyNewReply(int postID, object replyList)
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Clients.Group(courseID.ToString()).addNewReply(postID, replyList);
        }

        public void NotifyNewPost(object post)
        {
            //verify that the post is legit before pushing out to listeners
            //will match the post information with the cookie authkey and the database event.
            JObject feedPost = JObject.FromObject(post);
            int eventLogId = (int)feedPost.SelectToken("EventId");
            int courseID = int.Parse(Context.QueryString["courseID"]);
            int senderId = (int)feedPost.SelectToken("SenderId");
            string senderFullName = (string)feedPost.SelectToken("SenderName");

            //cookie authkey            
            string authKey = "";

            try
            {
                authKey = Context.Request.Cookies["AuthKey"].Value.Split('=').Last();
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
                new[] { profile.FullName, senderFullName, authUserProfile.FullName }.All(pfn => pfn == profile.FullName)) //the user full name has to match
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
    }
}