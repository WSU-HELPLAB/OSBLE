using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.DiscussionAssignment;

namespace OSBLE.Models.ViewModels
{
    public class GeneralPost
    {
        public int DiscussionPostId;
        public CourseUser CourseUser;
        public string Content;
        public string DisplayName;
    }
    public class DiscussionPostViewModel : GeneralPost
    {
        public List<GeneralPost> Replies;
        public DiscussionPostViewModel()
        {
            Replies = new List<GeneralPost>();
        }


        /// <summary>
        /// This function takes a list of discussion posts and sets them as GeneralPosts to be used in the view
        /// </summary>
        /// <param name="replies"></param>
        public void SetReplies(List<DiscussionPost> replies)
        {
            foreach (DiscussionPost reply in replies)
            {
                Replies.Add(new GeneralPost() { DiscussionPostId = reply.ID, Content = reply.Content, CourseUser = reply.CourseUser });
            }
        }
    }
}
