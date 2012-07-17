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
                    if (RoleName != null && RoleName != "")
                    {
                        returnValue = "(" + RoleName + ")" + returnValue;
                    }
                    else
                    {
                        if (CourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.Student) //Don't display student roles, they are obvious.
                        {
                            returnValue = "(" + CourseUser.AbstractRole.Name + ") " + returnValue;
                        }
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
