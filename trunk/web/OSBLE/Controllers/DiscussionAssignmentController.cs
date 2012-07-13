using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;

namespace OSBLE.Controllers
{
    public class DiscussionAssignmentController : OSBLEController
    {
        /// <summary>
        /// Returns true if the posters name should be Anonymized for a discussion assignment
        /// </summary>
        /// <param name="currentUserRoleId">The current users AbstractRoleID</param>
        /// <param name="postersUserRoleId">The posters AbstractRoleID</param>
        /// <param name="discussionSetting">The assignments discussion settings</param>
        /// <returns></returns>
        public bool AnonymizeNameForDiscussion(int currentUserRoleId, int postersUserRoleId, DiscussionSetting discussionSetting)
        {
            bool Anonymous = false;

            int studentRoleId = (int)CourseRole.CourseRoles.Student;
            int moderatorRoleId = (int)CourseRole.CourseRoles.Moderator;
            int instructorRoleId = (int)CourseRole.CourseRoles.Instructor;
            int taRoleId = (int)CourseRole.CourseRoles.TA;

            //If TAsCanPostToAllDiscussions, treat them as instructors to students. Otherwise treat them as moderators to students. 
            if (postersUserRoleId == taRoleId)
            {
                if (discussionSetting.TAsCanPostToAllDiscussions)
                {
                    postersUserRoleId = instructorRoleId;
                }
                else
                {
                    postersUserRoleId = moderatorRoleId;
                }
            }

            if (discussionSetting.HasAnonymousStudentsToStudents && currentUserRoleId == studentRoleId && postersUserRoleId == studentRoleId)
            {
                Anonymous = true;
            }
            else if (discussionSetting.HasAnonymousInstructorsToStudents && currentUserRoleId == studentRoleId && postersUserRoleId == instructorRoleId)
            {
                Anonymous = true;
            }
            else if (discussionSetting.HasAnonymousModeratorsToStudents && currentUserRoleId == studentRoleId && postersUserRoleId == moderatorRoleId)
            {
                Anonymous = true;
            }
            else if (discussionSetting.HasAnonymousStudentsToModerators && currentUserRoleId == moderatorRoleId && postersUserRoleId == studentRoleId)
            {
                Anonymous = true;
            }

            return Anonymous;
        }

        /// <summary>
        /// Returns true if the user should be anonymized for a critical review discussion
        /// </summary>
        /// <param name="currentUserRoleId">Current users AbstractRoleId</param>
        /// <param name="targetUserRoleId">Poster's AbstractRoleId</param>
        /// <param name="assignment">The Critical Review Discussion assignment</param>
        /// <param name="isAuthor">This value should be true if the poster if an author of the reviewed document</param>
        /// <param name="isReviewer">This value should be true if the poster if a reviewer of the document.</param>
        /// <returns></returns>
        public bool AnonymizeNameForCriticalReviewDiscussion(int currentUserRoleId, int targetUserRoleId, 
            Assignment assignment, bool posterIsAuthor, bool posterIsReviewer, bool currentUserIsAuthor, bool currentUserIsReviewer)
        {
            bool Anonymous = false;

            CriticalReviewSettings crSettings = assignment.PreceedingAssignment.CriticalReviewSettings;
            if(crSettings.AnonymizeAuthorToReviewer && currentUserIsReviewer && posterIsAuthor)
            {
                Anonymous = true;
            }
            else if(crSettings.AnonymizeReviewerToAuthor && currentUserIsAuthor && posterIsReviewer)
            {
                Anonymous = true;
            }
            //Purposefully not handling crSettings.AnonymizeReviewerToReviewers as this is property is used for the reviewed document not discussions. There is another
            //setting in place to anonmize reviewers to reviewers in discussion settins.


            //Anonymize if Anonymous is true or AnonymizeNameForDiscussion (anonymize based on discussion settings) is true.
            return (Anonymous || AnonymizeNameForDiscussion(currentUserRoleId, targetUserRoleId, assignment.DiscussionSettings));
        }

        // GET: /DiscussionAssignment/
        public ActionResult Index(int assignmentId, int discussionTeamId)
        {

            //checking if ids are good
            Assignment assignment = null;
            DiscussionTeam discussionTeam = null;
            if (discussionTeamId > 0 && assignmentId > 0)
            {

                assignment = db.Assignments.Find(assignmentId);
                discussionTeam = (from dt in assignment.DiscussionTeams
                                  where dt.ID == discussionTeamId
                                  select dt).FirstOrDefault();
            }

            //if ids are good and returned values, then confirm ActiveCourseUser belongs to that discussion team
            bool isInDiscussionTeam = false;
            if (assignment != null && discussionTeam != null)
            {

                foreach (TeamMember tm in discussionTeam.GetAllTeamMembers())
                {
                    if (tm.CourseUserID == ActiveCourseUser.ID)
                    {
                        isInDiscussionTeam = true;
                        break;
                    }
                }
            }



            //if ActiveCourseUser belongs to the discussionTeam, continue. Otherwise kick them out.
            if (isInDiscussionTeam)
            {
                List<DiscussionPostViewModel> DiscussionPostViewModelList = new List<DiscussionPostViewModel>();
                List<ReplyViewModel> ReplyViewModelList = new List<ReplyViewModel>();


                if (assignment.HasDiscussionTeams) //If there are discusison teams, we must filter our post queries by discussionTeamId
                {
                    if (assignment.Type == AssignmentTypes.CriticalReviewDiscussion) //CRDs have special permissions that must be checked.
                    {
                        bool currentUserIsAuthor = discussionTeam.AuthorTeam.TeamMembers.Where(tm => tm.CourseUserID == ActiveCourseUser.ID).ToList().Count > 0;
                        bool currentUserIsReviewer = discussionTeam.Team.TeamMembers.Where(tm => tm.CourseUserID == ActiveCourseUser.ID).ToList().Count > 0;


                        List<DiscussionPost> AuthorDiscussionPosts = (from Team authorTeam in db.Teams
                                                                      join TeamMember member in db.TeamMembers on authorTeam.ID equals member.TeamID
                                                                      join DiscussionPost post in db.DiscussionPosts on member.CourseUserID equals post.CourseUserID
                                                                      where authorTeam.ID == discussionTeam.AuthorTeamID &&
                                                                      post.DiscussionTeamID == discussionTeamId
                                                                      select post).ToList();

                        List<DiscussionPost> ReviewerPosts =
                                 (from Team reviewTeam in db.Teams
                                  join TeamMember member in db.TeamMembers on reviewTeam.ID equals member.TeamID
                                  join DiscussionPost post in db.DiscussionPosts on member.CourseUserID equals post.CourseUserID
                                  where reviewTeam.ID == discussionTeam.TeamID &&
                                  post.DiscussionTeamID == discussionTeamId
                                  select post).ToList();

                        //We want a list of all posts made by NonTeamMembers (Meaning they are not TeamMembers of AuthorTeam or Team) for this disucssionTeamId
                        //Goal: get all posts that's not written by the document author OR document reviewer (e.g. must be a non-student)
                        var NonTeamQuery = from post in db.DiscussionPosts
                                           where post.DiscussionTeamID == discussionTeamId
                                           && post.CourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.Student
                                           select post;

                        //if TAs cannot post to all discussions, filter out TA's posts as they will be duplicate posts from ReviewerPosts 
                        if (!assignment.DiscussionSettings.TAsCanPostToAllDiscussions)
                        {
                            NonTeamQuery = from f in NonTeamQuery
                                           where f.CourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.TA &&
                                           f.CourseUser.AbstractRoleID != (int)CourseRole.CourseRoles.Moderator
                                           select f;
                        }

                        List<DiscussionPost> NonTeamPosts = NonTeamQuery.ToList();





                        //Adding all the posts into dpvms. 
                        foreach (DiscussionPost dp in AuthorDiscussionPosts)
                        {
                            if (dp.ParentPostID == null) //adding parent posts
                            {
                                DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                                dpvm.Anonymize = AnonymizeNameForCriticalReviewDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment, true, false,
                                                                                            currentUserIsAuthor, currentUserIsReviewer);
                                dpvm.Content = dp.Content;
                                dpvm.CourseUser = dp.CourseUser;
                                dpvm.DiscussionPostId = dp.ID;
                                dpvm.Posted = dp.Posted;
                                DiscussionPostViewModelList.Add(dpvm);
                            }
                            else //adding replies
                            {
                                ReplyViewModel reply = new ReplyViewModel();
                                reply.Anonymize = AnonymizeNameForCriticalReviewDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment, true, false,
                                                                                            currentUserIsAuthor, currentUserIsReviewer);
                                reply.Content = dp.Content;
                                reply.CourseUser = dp.CourseUser;
                                reply.DiscussionPostId = dp.ID;
                                reply.ParentPostID = (int)dp.ParentPostID;
                                reply.Posted = dp.Posted;
                                ReplyViewModelList.Add(reply);
                            }
                        }

                        foreach (DiscussionPost dp in ReviewerPosts)
                        {
                            //Checking to see if post is already in list to avoid duplicate posts. (Duplicate posts are
                            //caused my some members being part of both "DiscussionTeam.Team" & "DiscussionTeam.AuthorTeam"
                            DiscussionPostViewModel existingAuthorPost = DiscussionPostViewModelList.Where(dpvm => dpvm.DiscussionPostId == dp.ID).FirstOrDefault();
                            if (dp.ParentPostID == null && existingAuthorPost == null) //adding parent posts if existingAuthorPost is not found.
                            {
                                DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                                //Reviewers can potentially be TA/Moderators. We do not want to anonymize TA/Moderators
                                dpvm.Anonymize = AnonymizeNameForCriticalReviewDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment, false, true,
                                                                                            currentUserIsAuthor, currentUserIsReviewer);
                                dpvm.Content = dp.Content;
                                dpvm.CourseUser = dp.CourseUser;
                                dpvm.DiscussionPostId = dp.ID;
                                dpvm.Posted = dp.Posted;
                                DiscussionPostViewModelList.Add(dpvm);
                            }
                            else if (dp.ParentPostID == null && existingAuthorPost != null)
                            {
                                //change value of Anonymize because the current poster is a reviewer and author. So or'ing previous value with new anon value.
                                int indexer = DiscussionPostViewModelList.IndexOf(existingAuthorPost);
                                DiscussionPostViewModelList[indexer].Anonymize = DiscussionPostViewModelList[indexer].Anonymize ||
                                                                                    AnonymizeNameForCriticalReviewDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID,
                                                                                            assignment, false, true, currentUserIsAuthor, currentUserIsReviewer);

                            }
                            else //dp.ParentPostID != null (its a reply)
                            {
                                ReplyViewModel existingAuthorReply = ReplyViewModelList.Where(reply => reply.DiscussionPostId == dp.ID).FirstOrDefault();
                                if (existingAuthorReply == null)//adding replies
                                {
                                    ReplyViewModel reply = new ReplyViewModel();
                                    reply.Anonymize = AnonymizeNameForCriticalReviewDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment, false, true,
                                                                                            currentUserIsAuthor, currentUserIsReviewer);
                                    reply.Content = dp.Content;
                                    reply.CourseUser = dp.CourseUser;
                                    reply.DiscussionPostId = dp.ID;
                                    reply.ParentPostID = (int)dp.ParentPostID;
                                    reply.Posted = dp.Posted;
                                    ReplyViewModelList.Add(reply);
                                }
                                else if (existingAuthorReply != null)
                                {
                                    //change value of Anonymize by ||ing dpvm.Anonymize with the reviewerAnonSetting. That way the user is masked for author
                                    //or for reviewer, if they are both.
                                    int indexer = ReplyViewModelList.IndexOf(existingAuthorReply);
                                    ReplyViewModelList[indexer].Anonymize = ReplyViewModelList[indexer].Anonymize ||
                                                                                AnonymizeNameForCriticalReviewDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID,
                                                                                     assignment, false, true, currentUserIsAuthor, currentUserIsReviewer);
                                }
                            }


                        }

                        foreach (DiscussionPost dp in NonTeamPosts)
                        {
                            if (dp.ParentPostID == null) //adding parent posts
                            {
                                DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                                dpvm.Anonymize = AnonymizeNameForDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment.DiscussionSettings);
                                dpvm.Content = dp.Content;
                                dpvm.CourseUser = dp.CourseUser;
                                dpvm.DiscussionPostId = dp.ID;
                                dpvm.Posted = dp.Posted;
                                DiscussionPostViewModelList.Add(dpvm);
                            }
                            else
                            {
                                ReplyViewModel reply = new ReplyViewModel();
                                reply.Anonymize = AnonymizeNameForDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment.DiscussionSettings);
                                reply.Content = dp.Content;
                                reply.CourseUser = dp.CourseUser;
                                reply.DiscussionPostId = dp.ID;
                                reply.ParentPostID = (int)dp.ParentPostID;
                                reply.Posted = dp.Posted;
                                ReplyViewModelList.Add(reply);
                            }

                        }
                    }
                    else //normal discussion assignment with teams. 
                    {
                        List<DiscussionPost> AllPosts = (from post in db.DiscussionPosts
                                                         where post.ParentPostID == null &&
                                                         post.DiscussionTeamID == discussionTeamId
                                                         select post).ToList();

                        foreach (DiscussionPost dp in AllPosts)
                        {
                            if (dp.ParentPostID == null) //adding parent posts
                            {
                                DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                                dpvm.Anonymize = AnonymizeNameForDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment.DiscussionSettings);
                                dpvm.Content = dp.Content;
                                dpvm.CourseUser = dp.CourseUser;
                                dpvm.DiscussionPostId = dp.ID;
                                dpvm.Posted = dp.Posted;
                                DiscussionPostViewModelList.Add(dpvm);
                            }
                            else //adding replies
                            {
                                ReplyViewModel reply = new ReplyViewModel();
                                reply.Anonymize = AnonymizeNameForDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment.DiscussionSettings);
                                reply.Content = dp.Content;
                                reply.CourseUser = dp.CourseUser;
                                reply.DiscussionPostId = dp.ID;
                                reply.ParentPostID = (int)dp.ParentPostID;
                                reply.Posted = dp.Posted;
                                ReplyViewModelList.Add(reply);
                            }
                        }
                    }
                }
                else //Any discussion that does not have dsicussion teams is a classwide discussion, so filter posts on AssignmentID rather than DiscussionTeamID
                {
                    List<DiscussionPost> AllPosts = (from post in db.DiscussionPosts
                                                     where post.ParentPostID == null &&
                                                     post.AssignmentID == assignment.ID
                                                     select post).ToList();

                    foreach (DiscussionPost dp in AllPosts)
                    {
                        if (dp.ParentPostID == null) //adding parent posts
                        {
                            DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                            dpvm.Anonymize = AnonymizeNameForDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment.DiscussionSettings);
                            dpvm.Content = dp.Content;
                            dpvm.CourseUser = dp.CourseUser;
                            dpvm.DiscussionPostId = dp.ID;
                            dpvm.Posted = dp.Posted;
                            DiscussionPostViewModelList.Add(dpvm);
                        }
                        else //adding replies
                        {
                            ReplyViewModel reply = new ReplyViewModel();
                            reply.Anonymize = AnonymizeNameForDiscussion(ActiveCourseUser.AbstractRoleID, dp.CourseUser.AbstractRoleID, assignment.DiscussionSettings);
                            reply.Content = dp.Content;
                            reply.CourseUser = dp.CourseUser;
                            reply.DiscussionPostId = dp.ID;
                            reply.ParentPostID = (int)dp.ParentPostID;
                            reply.Posted = dp.Posted;
                            ReplyViewModelList.Add(reply);
                        }
                    }
                }

                //associating each replyviewmodel with its parent post
                foreach (DiscussionPostViewModel dpvm in DiscussionPostViewModelList)
                {
                    dpvm.Replies = (from replies in ReplyViewModelList
                                    where replies.ParentPostID == dpvm.DiscussionPostId
                                    select replies).ToList();
                }


                //Checking if its users first post
                ViewBag.IsFirstPost = (from dpvm in DiscussionPostViewModelList
                                       where dpvm.CourseUser.ID == ActiveCourseUser.ID
                                       select dpvm).Count() == 0;


                //assigning a header value.
                if (assignment.HasDiscussionTeams)
                {
                    ViewBag.DiscussionHeader = assignment.AssignmentName + "- " + discussionTeam.TeamName;
                }
                else
                {
                    ViewBag.DiscussionHeader = assignment.AssignmentName;
                }

                //for CRD assignment types we need a list of all discussions they can participate in for navigation.
                if (assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
                {
                    List<DiscussionTeam> DiscussionTeamList = new List<DiscussionTeam>();
                    //Generating a list of discussion assignments that the current user belongs to
                    foreach (DiscussionTeam dt in assignment.DiscussionTeams)
                    {
                        foreach (TeamMember tm in dt.GetAllTeamMembers())
                        {
                            if (tm.CourseUserID == ActiveCourseUser.ID)
                            {
                                DiscussionTeamList.Add(dt);
                                break;
                            }
                        }
                    }
                    ViewBag.DiscussionTeamList = DiscussionTeamList.OrderBy(dt => dt.TeamName).ToList();
                }

                ViewBag.DiscussionPostViewModelList = DiscussionPostViewModelList.OrderBy(dpvm => dpvm.Posted).ToList();
                ViewBag.ActiveCourse = ActiveCourseUser;
                ViewBag.Assignment = assignment;
                ViewBag.DiscussionTeamID = discussionTeam.ID;
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentId });
            }
        }
        /// <summary>
        /// Displays the Discussion view for Teachers
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="courseUserId">the CourseUser.ID of the student you want to "Highlight". 0 may be passed in highlighting is unnecessary</param>
        /// <param name="postOrReply">postOrReply is used as enumerable. 0 = Posts, 1 = Replies, 2 = Both, 3 = No Selector</param>
        /// <param name="discussionTeamID">The discussion team id for discussion to beto viewed. If it is a classwide discussion, then any dt can be sent.</param>
        /// <returns></returns>
        [CanGradeCourse]
        public ActionResult TeacherIndex(int assignmentId, int discussionTeamID, int courseUserId = 0, int postOrReply = 3)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && ActiveCourseUser.AbstractRole.CanGrade)
            {
                List<DiscussionPost> posts = null;
                CourseUser student;
                DiscussionTeam discussionTeam = (from dt in assignment.DiscussionTeams
                                                 where dt.ID == discussionTeamID
                                                 select dt).FirstOrDefault();

                List<DiscussionPostViewModel> DiscussionPostViewModelList = new List<DiscussionPostViewModel>();
                List<ReplyViewModel> ReplyViewModelList = new List<ReplyViewModel>();

                //Collecting posts and setting up DiscussionTeamList.
                if (assignment.HasDiscussionTeams)
                {
                    //Only want posts associated with discussionTeamID
                    ViewBag.DiscussionTeamList = assignment.DiscussionTeams.OrderBy(dt => dt.TeamName).ToList();

                    posts = (from post in db.DiscussionPosts
                                 .Include("Replies")
                                 .Include("CourseUser")
                                 where post.AssignmentID == assignment.ID &&
                                 post.DiscussionTeamID == discussionTeamID
                                 select post).ToList();
                }
                else
                {
                    //want all posts associated with assignmentId
                    posts = (from post in db.DiscussionPosts
                                .Include("Replies")
                                .Include("CourseUser")
                                where post.AssignmentID == assignment.ID
                                orderby post.Posted
                                select post).ToList();
                }

                if (postOrReply == 3 || courseUserId <= 0)  //If postOrReply is 3, we want no selections - so setting student to null.
                {
                    student = null;
                }
                else
                {
                    student = db.CourseUsers.Find(courseUserId);
                }

                if (assignment.HasDiscussionTeams)
                {
                    ViewBag.DiscussionHeader = assignment.AssignmentName + " - " + discussionTeam.TeamName;
                }
                else
                {
                    ViewBag.DiscussionHeader = assignment.AssignmentName;
                }

                //creating a view mode for each discussionpost
                foreach(DiscussionPost dp in posts)
                {
                    if(dp.ParentPostID == null)
                    {
                        DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                        dpvm.Anonymize = false; //never anonymize for instructors/TA
                        dpvm.Content = dp.Content;
                        dpvm.CourseUser = dp.CourseUser;
                        dpvm.DiscussionPostId = dp.ID;
                        dpvm.Posted = dp.Posted;
                        DiscussionPostViewModelList.Add(dpvm);
                    }
                    else
                    {
                        ReplyViewModel reply = new ReplyViewModel();
                        reply.Anonymize = false;
                        reply.Content = dp.Content;
                        reply.CourseUser = dp.CourseUser;
                        reply.DiscussionPostId = dp.ID;
                        reply.Posted = dp.Posted;
                        reply.ParentPostID = (int)dp.ParentPostID;
                        ReplyViewModelList.Add(reply);
                    }
                }

                //associating each replyviewmodel with its parent post
                foreach (DiscussionPostViewModel dpvm in DiscussionPostViewModelList)
                {
                    dpvm.Replies = (from replies in ReplyViewModelList
                                    where replies.ParentPostID == dpvm.DiscussionPostId
                                    select replies).ToList();
                }

                ViewBag.DiscussionPostViewModelList = DiscussionPostViewModelList.OrderBy(dpvm => dpvm.Posted).ToList();
                ViewBag.PostOrReply = postOrReply;
                ViewBag.Posts = posts;
                ViewBag.Student = student;
                ViewBag.Assignment = assignment;
                ViewBag.IsFirstPost = false;
                ViewBag.ActiveCourse = ActiveCourseUser;
                ViewBag.DiscussionTeamID = discussionTeam.ID;
                return View("Index");
            }
            return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentId });
        }

        [HttpPost]
        public ActionResult NewPost(DiscussionPost newPost)
        {
            if (newPost != null && newPost.Content != null && newPost.AssignmentID > 0 && newPost.DiscussionTeamID > 0)
            {
                Assignment assignment = db.Assignments.Find(newPost.AssignmentID);
                newPost.CourseUserID = ActiveCourseUser.ID;
                db.DiscussionPosts.Add(newPost);
                db.SaveChanges();
            }
            return Redirect(Request.UrlReferrer.ToString());
        }

        [HttpPost]
        public ActionResult NewReply(DiscussionPost reply)
        {
            if (reply.Content != null && reply.ParentPostID > 0 && reply.DiscussionTeamID > 0 && reply.AssignmentID > 0)
            {
                Assignment assignment = db.Assignments.Find(reply.AssignmentID);
                reply.CourseUserID = ActiveCourseUser.ID;
                db.DiscussionPosts.Add(reply);
                db.SaveChanges();
            }
            return Redirect(Request.UrlReferrer.ToString());
        }

        [HttpGet, FileCache(Duration = 3600)]
        public FileStreamResult ProfilePictureForDiscussion(int course, int userProfile)
        {
            // File Stream that will ultimately contain profile picture.
            FileStream pictureStream;

            // User Profile object of user we are trying to get a picture of
            UserProfile u = db.UserProfiles.Find(userProfile);

            // A role for both our current user and
            // the one we're trying to see
            AbstractRole ourRole = currentCourses.Where(c => c.AbstractCourseID == course).Select(c => c.AbstractRole).FirstOrDefault();
            AbstractRole theirRole = db.CourseUsers.Where(c => (c.AbstractCourseID == course) && (c.UserProfileID == userProfile)).Select(c => c.AbstractRole).FirstOrDefault();

            // Show picture if user is requesting their own profile picture or they have the right to view the profile picture
            if (userProfile == CurrentUser.ID ||
                // Current user's CourseRole
                ourRole != null &&
                // Target user's CourseRole
                theirRole != null &&
                // If current user is not anonymous or other user is instructor/TA, show picture
                (!(ourRole.Anonymized) || theirRole.CanGrade)
               )
            {
                pictureStream = FileSystem.GetProfilePictureOrDefault(u);
            }
            else
            {
                // Default to blue OSBLE guy picture.
                pictureStream = FileSystem.GetDefaultProfilePicture();
            }

            return new FileStreamResult(pictureStream, "image/jpeg");
        }
    }
}
