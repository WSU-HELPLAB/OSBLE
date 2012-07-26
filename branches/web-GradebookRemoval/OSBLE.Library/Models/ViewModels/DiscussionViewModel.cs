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
        public UserProfile UserProfile;
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
                    returnValue = UserProfile.FirstName + " " + UserProfile.LastName;
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

    public class DiscussionViewModel
    {
        private DiscussionTeam discussionTeam;
        private CourseUser currentUser;
        private List<ReplyViewModel> ReplyViewModels;
        private List<DiscussionPostViewModel> discussionPostViewModels;
        public List<DiscussionPostViewModel> DiscussionPostViewModels
        {
            get
            {
                return discussionPostViewModels.OrderBy(dpvm => dpvm.Posted).ToList();
            }
        }

        /// <summary>
        /// This class will set up a list of DiscussionPostViewModels for a discussion based assignment. Note: Virtual calls will not work off of public
        /// attributes within this view model, unless you append to the queries that gather them a ".Include("VirtualMember")" statement.
        /// </summary>
        /// <param name="DiscussionTeam">The discussion team to generate view models for</param>
        /// <param name="CurrentUser">The current user</param>
        public DiscussionViewModel(DiscussionTeam DiscussionTeam, CourseUser CurrentUser)
        {
            currentUser = CurrentUser;
            discussionTeam = DiscussionTeam;
            discussionPostViewModels = new List<DiscussionPostViewModel>();
            ReplyViewModels = new List<ReplyViewModel>();

            if (discussionTeam.Assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
            {
                InitializeViewModelForCriticalReviewDiscussion();
            }
            else
            {
                InitializeViewModelForDiscussionAssignment();
            }
        }


        /// <summary>
        /// This function prepares DiscussionPostViewModels for a Critical Review Discussion. 
        /// </summary>
        private void InitializeViewModelForCriticalReviewDiscussion()
        {

            bool currentUserIsAuthor = discussionTeam.AuthorTeam.TeamMembers.Where(tm => tm.CourseUserID == currentUser.ID).ToList().Count > 0;
            bool currentUserIsReviewer = discussionTeam.Team.TeamMembers.Where(tm => tm.CourseUserID == currentUser.ID).ToList().Count > 0;

            using (OSBLEContext db = new OSBLEContext())
            {
                //Gathering all posts made by students on discussionTeam.AuthorTeam.
                List<DiscussionPost> AuthorPosts =
                                                    (from Team authorTeam in db.Teams
                                                     join TeamMember member in db.TeamMembers on authorTeam.ID equals member.TeamID
                                                     join DiscussionPost post in db.DiscussionPosts
                                                        .Include("CourseUser")
                                                        .Include("CourseUser.AbstractRole")
                                                     on member.CourseUserID equals post.CourseUserID
                                                     where authorTeam.ID == discussionTeam.AuthorTeamID &&
                                                     post.DiscussionTeamID == discussionTeam.ID
                                                     select post).ToList();

                //Gathering  all posts made by students on discussionTeam.Team. Note: (Some of these will be duplcates from AuthorDiscussionPosts. Handle later)
                List<DiscussionPost> ReviewerPosts =
                                                     (from Team reviewTeam in db.Teams
                                                      join TeamMember member in db.TeamMembers on reviewTeam.ID equals member.TeamID
                                                      join DiscussionPost post in db.DiscussionPosts
                                                        .Include("CourseUser")
                                                        .Include("CourseUser.AbstractRole")
                                                      on member.CourseUserID equals post.CourseUserID
                                                      where reviewTeam.ID == discussionTeam.TeamID &&
                                                      post.DiscussionTeamID == discussionTeam.ID &&
                                                      post.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                                      select post).ToList();

                //Gathering  all non-student posts. (TAs, moderators, instructors)
                List<DiscussionPost> NonStudentPosts = (from DiscussionPost post in db.DiscussionPosts
                                                                .Include("CourseUser")
                                                                .Include("CourseUser.AbstractRole")
                                                        where post.DiscussionTeamID == discussionTeam.ID
                                                        && post.CourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.Student
                                                        select post).ToList();


                //Now that we have our lists, we will remove duplicates and convert each lists based off Author, Reviewers, Author/Reviewers, and finally NonStudent
                List<DiscussionPost> AuthorReviewers = AuthorPosts.Intersect(ReviewerPosts, new DiscussionPostComparer()).ToList();

                //Converting AuthorPosts to ViewModels, then authorReviewers, then Reviewers, and finally NonSTudent posts. 
                ConvertPostsToViewModel(AuthorPosts.Except(AuthorReviewers, new DiscussionPostComparer()).ToList(), "Author", currentUserIsAuthor, currentUserIsReviewer);
                ConvertPostsToViewModel(ReviewerPosts.Except(AuthorReviewers, new DiscussionPostComparer()).ToList(), "Reviewer", currentUserIsAuthor, currentUserIsReviewer);
                ConvertPostsToViewModel(AuthorReviewers.ToList(), "Author/Reviewer", currentUserIsAuthor, currentUserIsReviewer);
                ConvertPostsToViewModel(NonStudentPosts.ToList(), null, currentUserIsAuthor, currentUserIsReviewer);

                //Now adding all the replies from ReplyListViewModels into their correct discussionPost
                OrganizeReplies();
            }
        }


        /// <summary>
        /// This function prepares DiscussionPostViewModels for a Discussion Assignment. 
        /// </summary>
        private void InitializeViewModelForDiscussionAssignment()
        {
            List<DiscussionPost> AllPosts;
            using (OSBLEContext db = new OSBLEContext())
            {
                //If the assignment has discussion teams, we want to grab only DiscussionPosts made by a specific DiscussionTeam.ID. 
                //Otherwise, we want to grab all the posts for the entire class.
                if (discussionTeam.Assignment.HasDiscussionTeams)
                {
                    AllPosts = (from post in db.DiscussionPosts
                                .Include("CourseUser")
                                .Include("CourseUser.AbstractRole")
                                where post.DiscussionTeamID == discussionTeam.ID
                                select post).ToList();
                }
                else
                {
                    AllPosts = (from post in db.DiscussionPosts
                                .Include("CourseUser")
                                .Include("CourseUser.AbstractRole")
                                where post.AssignmentID == discussionTeam.AssignmentID
                                select post).ToList();
                }
                ConvertPostsToViewmodel(AllPosts);
                OrganizeReplies();
            }
        }

        /// <summary>
        /// This method associates ReplyViewModels with their DiscussionPost in DiscussionPostViewModels
        /// </summary>
        private void OrganizeReplies()
        {
            foreach (DiscussionPostViewModel dpvm in discussionPostViewModels)
            {
                dpvm.Replies = (from reply in ReplyViewModels
                                where reply.ParentPostID == dpvm.DiscussionPostId
                                select reply).ToList();
            }
        }


        /// <summary>
        /// This function takes a list of DiscussionPost and converts them into DiscussionPostViewModels. (Handles all anonmization, roles, etc)
        /// </summary>
        /// <param name="discussionPosts">The list of discussionposts to convert</param>
        private void ConvertPostsToViewmodel(List<DiscussionPost> discussionPosts)
        {
            //Same functionality, but send in dummy values that will not be used.
            ConvertPostsToViewModel(discussionPosts, null, false, false);
        }


        /// <summary>
        /// This function takes a list of DiscussionPost and converts them into DiscussionPostViewModels. In order to appropriately handle anonmization,
        /// other parameters are required.
        /// </summary>
        /// <param name="discussionPosts">The list of discussionposts to convert</param>
        /// <param name="RoleName">The rolename for the group of discussionposts. Non-CourseRoles only. I.e. "Author" or "Reviewer"</param>
        /// <param name="currentUserisAuthor">boolean value indicating whether current user is an author (used for anomization)</param>
        /// <param name="currentUserIsReviewer">boolean value indicating whether current user is a reviewer (used for anomization)</param>
        private void ConvertPostsToViewModel(List<DiscussionPost> discussionPosts, string RoleName, bool currentUserisAuthor, bool currentUserIsReviewer)
        {
            //First, we check the rolename to see if poster is author and/or reviewer
            bool posterIsAuthor = RoleName != null && RoleName.Contains("Author");
            bool posterIsReviewer = RoleName != null && RoleName.Contains("Reviewer");

            //Next, iterate over all discussion posts, and depending on the assignment type anonmize them correctly. Then, depending on the value of
            //ParentPostID, assign them to a DiscussionPostViewModels or ReplyViewModels
            foreach (DiscussionPost dp in discussionPosts)
            {
                bool anonymizePost = false;
                if (discussionTeam.Assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
                {
                    anonymizePost = AnonymizeNameForCriticalReviewDiscussion(dp.CourseUser, currentUser, discussionTeam.Assignment ,posterIsAuthor, posterIsReviewer, currentUserisAuthor, currentUserIsReviewer);
                }
                else //Regular discussion assignment.
                {
                    anonymizePost = AnonymizeNameForDiscussion(dp.CourseUser, currentUser, discussionTeam.Assignment.DiscussionSettings);
                }

                if (dp.ParentPostID == null) //post
                {
                    DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                    dpvm.poster.Anonymize = anonymizePost;
                    dpvm.poster.HideRole = discussionTeam.Assignment.DiscussionSettings.HasHiddenRoles;
                    dpvm.poster.RoleName = RoleName;
                    dpvm.poster.CourseUser = dp.CourseUser;
                    dpvm.poster.UserProfile = dp.CourseUser.UserProfile;
                    dpvm.Content = dp.Content;
                    dpvm.DiscussionPostId = dp.ID;
                    dpvm.Posted = dp.Posted;
                    discussionPostViewModels.Add(dpvm);
                }
                else //reply
                {
                    ReplyViewModel reply = new ReplyViewModel();
                    reply.poster.Anonymize = anonymizePost;
                    reply.poster.CourseUser = dp.CourseUser;
                    reply.Posted = dp.Posted;
                    reply.poster.HideRole = discussionTeam.Assignment.DiscussionSettings.HasHiddenRoles;
                    reply.poster.RoleName = RoleName;
                    reply.poster.UserProfile = dp.CourseUser.UserProfile;
                    reply.Content = dp.Content;
                    reply.DiscussionPostId = dp.ID;
                    reply.ParentPostID = (int)dp.ParentPostID;
                    ReplyViewModels.Add(reply);
                }
            }
        }


        /// <summary>
        /// Returns true if the posters name should be Anonymized for a discussion assignment
        /// </summary>
        /// <param name="currentUser">The current users</param>
        /// <param name="poster">The posters courseuser</param>
        /// <param name="discussionSetting">The assignments discussion settings</param>
        /// <returns></returns>
        public static bool AnonymizeNameForDiscussion(CourseUser poster, CourseUser currentUser, DiscussionSetting discussionSettings)
        {
            bool Anonymous = false;
            //Don't want to set anonymous permissions if the poster is the current user
            //Additionally, we do not want to anonmize for TA or Instructors

            if (poster.ID != currentUser.ID &&
                currentUser.AbstractRoleID != (int)CourseRole.CourseRoles.Instructor &&
                currentUser.AbstractRoleID != (int)CourseRole.CourseRoles.TA)
            {

                //Checking role of currentUser
                bool currentUserIsStudent = currentUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student;
                bool currentUserIsModerator = currentUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator;

                //Checking role of poster. Note: If the poster is a TA, we treat them like a moderator or instructor depending on the value
                //of TAsCanPostToAllDiscussions
                bool posterIsStudent = poster.AbstractRoleID == (int)CourseRole.CourseRoles.Student;
                bool posterIsModerator = poster.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator ||
                        (!discussionSettings.TAsCanPostToAllDiscussions && poster.AbstractRoleID == (int)CourseRole.CourseRoles.TA);
                bool posterIsInstructor = poster.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor ||
                        (discussionSettings.TAsCanPostToAllDiscussions && poster.AbstractRoleID == (int)CourseRole.CourseRoles.TA); ;


                //if current user is a student, poster is a student, and student is anonymized to student
                if (discussionSettings.HasAnonymousStudentsToStudents && currentUserIsStudent && posterIsStudent)
                {
                    Anonymous = true;
                }
                //if current user is a student, poster is an instructor, and instructor is anonymized to student 
                else if (discussionSettings.HasAnonymousInstructorsToStudents && currentUserIsStudent && posterIsInstructor)
                {
                    Anonymous = true;
                }
                //if current user is a student, poster is an moderator, and moderator is anonymized to student 
                else if (discussionSettings.HasAnonymousModeratorsToStudents && currentUserIsStudent && posterIsModerator)
                {
                    Anonymous = true;
                }
                //if current user is a moderator, poster is a student, and student is anonymized to moderator 
                else if (discussionSettings.HasAnonymousStudentsToModerators && currentUserIsModerator && posterIsStudent)
                {
                    Anonymous = true;
                }
            }
            return Anonymous;
        }

        /// <summary>
        /// Returns true if the user should be anonymized for a critical review discussion
        /// </summary>
        /// <param name="currentUser">Current users</param>
        /// <param name="poster">Poster's courseuser</param>
        /// <param name="assignment">The Critical Review Discussion assignment</param>
        /// <param name="isAuthor">This value should be true if the poster if an author of the reviewed document</param>
        /// <param name="isReviewer">This value should be true if the poster if a reviewer of the document.</param>
        /// <returns></returns>
        public static bool AnonymizeNameForCriticalReviewDiscussion(CourseUser poster, CourseUser currentUser, Assignment assignment,
            bool posterIsAuthor, bool posterIsReviewer, bool currentUserIsAuthor, bool currentUserIsReviewer)
        {
            bool Anonymous = false;

            //Only attempt to anomize if current user is NOT an instructor or TA
            if (currentUser.AbstractRoleID != (int)CourseRole.CourseRoles.Instructor && currentUser.AbstractRoleID != (int)CourseRole.CourseRoles.TA)
            {
                CriticalReviewSettings criticalReviewSettings = assignment.PreceedingAssignment.CriticalReviewSettings;
                if (criticalReviewSettings.AnonymizeAuthorToReviewer && currentUserIsReviewer && posterIsAuthor)
                {
                    Anonymous = true;
                }
                else if (criticalReviewSettings.AnonymizeReviewerToAuthor && currentUserIsAuthor && posterIsReviewer)
                {
                    Anonymous = true;
                }
            }

            //For critical review discussions, we not only want to check the Assignment.PreceedingAssignment.CriticalReviewSettings 
            //but additionally, apply any anonmization based off of Assignment.DiscussionSettings
            return (Anonymous || AnonymizeNameForDiscussion(poster, currentUser, assignment.DiscussionSettings));
        }
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

    //Icomparer used for discusisonPosts
    public class DiscussionPostComparer : IEqualityComparer<DiscussionPost>
    {
        bool IEqualityComparer<DiscussionPost>.Equals(DiscussionPost x, DiscussionPost y)
        {
            // Check whether the compared objects reference the same data.        
            if (x.ID == y.ID)
                return true;
            return false;
        }

        public int GetHashCode(DiscussionPost obj)
        {
            return obj.ID;
        }
    }

}
