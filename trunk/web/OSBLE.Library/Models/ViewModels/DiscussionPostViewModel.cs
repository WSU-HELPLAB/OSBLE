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
    public class Poster
    {
        public bool Anonymize;
        public CourseUser CourseUser;
        public bool HideRole;
        public string RoleName;
        public string DisplayName
        {

            get
            {
                string returnValue = "";
                if (Anonymize)
                {
                    returnValue = "Anonymous " + CourseUser.ID;
                }
                else
                {
                    returnValue = CourseUser.UserProfile.FirstName + " " + CourseUser.UserProfile.LastName;
                }

                if (!HideRole)
                {
                    //Display RoleName only for students if there is one availble.
                    if (RoleName != null && RoleName != "" && CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student) 
                    {
                        returnValue = returnValue + " (" + RoleName + ")";
                    }
                    //We want to display the CourseRole for nonstudents if roles are to be shown. We never want to show (Student) as no-role indicates that.
                    else if (CourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.Student) 
                    {
                        returnValue = returnValue + " (" + CourseUser.AbstractRole.Name + ")";
                    }
                }
                return returnValue;
            }
        }
    }

    public class GeneralPost
    {
        public string Content;  
        public int DiscussionPostId;
        public DateTime Posted;
        public Poster poster;
        public GeneralPost()
        {
            poster = new Poster();
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
