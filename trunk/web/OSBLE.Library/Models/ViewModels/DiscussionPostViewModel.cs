using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.ViewModels
{
    public class GeneralPost
    {
        public bool Anonymize;
        public string Content;
        public CourseUser CourseUser;
        public int DiscussionPostId;
        public DateTime Posted;

        public string DisplayName {

            get
            {
                if (Anonymize)
                {
                    return "Anonymous " + CourseUser.ID;
                }
                else
                {
                    return CourseUser.UserProfile.FirstName + " " + CourseUser.UserProfile.LastName;
                }
            }
        }
    }

    public class ReplyViewModel : GeneralPost
    {
        public int ParentPostID;
    }
    public class DiscussionPostViewModel : GeneralPost
    {
        public DiscussionPostViewModel()
        {
            replies = new List<ReplyViewModel>();
        }

        private List<ReplyViewModel> replies;

        public List<ReplyViewModel> Replies { 
            get { return replies.OrderBy(r => r.Posted).ToList(); } 
            set { replies = value; } 
        }

    }
}
