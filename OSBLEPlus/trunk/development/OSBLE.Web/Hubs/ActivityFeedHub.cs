using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using OSBLE.Utility;

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
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Clients.Group(courseID.ToString()).addNewPost(courseID, post);
        }

        public void NotifyEditPost(int postID, string newContent)
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Clients.Group(courseID.ToString()).editPostContent(postID, newContent);
        }

        public void NotifyEditReply(int postID, int replyID, string newContent)
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Clients.Group(courseID.ToString()).editReplyContent(postID, replyID, newContent);
        }

        // Note: this method can also remove helpful marks
        public void NotifyAddMarkHelpful(int parentPostID, int replyPostID, int numHelpfulMarks)
        {
            int courseID = int.Parse(Context.QueryString["courseID"]);
            Clients.Group(courseID.ToString()).addMarkHelpful(parentPostID, replyPostID, numHelpfulMarks);
        }
    }
}