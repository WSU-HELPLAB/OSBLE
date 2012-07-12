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

        // GET: /DiscussionAssignment/
        public ActionResult Index(int assignmentId, int discussionTeamId)
        {

            //checking if ids are good
            Assignment assignment = null;
            DiscussionTeam discussionTeam = null;
            if (discussionTeamId > 0 && assignmentId > 0)
            {

                assignment= db.Assignments.Find(assignmentId);
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
            if(isInDiscussionTeam)
            {
                List<DiscussionPostViewModel> DiscussionPostViewModelList = new List<DiscussionPostViewModel>();
                List<ReplyViewModel> ReplyViewModelList = new List<ReplyViewModel>();

                //A boolean to determine if we should automatically anonymize certain names because the current user is a moderator
                bool AnonymizeForModerator = (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator 
                                                && assignment.DiscussionSettings.HasAnonymizationforModerators);


                if (assignment.HasDiscussionTeams) //If there are discusison teams, we must filter our post queries by discussionTeamId
                {
                    if (assignment.Type == AssignmentTypes.CriticalReviewDiscussion) //CRDs have special permissions that must be checked.
                    {       

                        //For critical reviews
                            //get posts/replies by authors for this discussionTeamId
                            //get posts/replies by reviewers for this discussionTeamId
                            //get posts/replies byeveryone that is a non author/reviewer for this discussionTeamId
                            //Create DiscussionPostViewModel for each post. Avoid duplicates on reviewers (as they can sometimes also be authors)
                            //Create ReplyViewModel for each reply. Avoid duplicates on reviewers (as they can sometimes also be authors)

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

                        List<DiscussionPost> NonTeamPosts = NonTeamQuery.ToList(); //foo.To
                        




                        //Adding all the posts into dpvms. 
                        foreach (DiscussionPost dp in AuthorDiscussionPosts)
                        {
                            if (dp.ParentPostID == null) //adding parent posts
                            {
                                DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                                dpvm.Anonymize = AnonymizeForModerator || assignment.DiscussionSettings.HasAnonymousAuthors || assignment.DiscussionSettings.HasAnonymousPosts;
                                dpvm.Content = dp.Content;
                                dpvm.CourseUser = dp.CourseUser;
                                dpvm.DiscussionPostId = dp.ID;
                                dpvm.Posted = dp.Posted;
                                DiscussionPostViewModelList.Add(dpvm);
                            }
                            else //adding replies
                            {
                                ReplyViewModel reply = new ReplyViewModel();
                                reply.Anonymize = AnonymizeForModerator || assignment.DiscussionSettings.HasAnonymousAuthors || assignment.DiscussionSettings.HasAnonymousPosts;
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
                            bool reviewerAnonSetting = (dp.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student) &&
                                                    (assignment.DiscussionSettings.HasAnonymousReviewers || assignment.DiscussionSettings.HasAnonymousPosts || AnonymizeForModerator);
                            if (dp.ParentPostID == null && existingAuthorPost == null) //adding parent posts if existingAuthorPost is not found.
                            {
                                DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                                //Reviewers can potentially be TA/Moderators. We do not want to anonymize TA/Moderators
                                dpvm.Anonymize = reviewerAnonSetting;
                                dpvm.Content = dp.Content;
                                dpvm.CourseUser = dp.CourseUser;
                                dpvm.DiscussionPostId = dp.ID;
                                dpvm.Posted = dp.Posted;
                                DiscussionPostViewModelList.Add(dpvm);   
                            }
                            else if (dp.ParentPostID == null && existingAuthorPost != null)
                            {
                                //change value of Anonymize by ||ing dpvm.Anonymize with the reviewerAnonSetting. That way the user is masked for author
                                //or for reviewer, if they are both.
                                int indexer = DiscussionPostViewModelList.IndexOf(existingAuthorPost);
                                DiscussionPostViewModelList[indexer].Anonymize = DiscussionPostViewModelList[indexer].Anonymize || reviewerAnonSetting;
                                
                            }
                            else //dp.ParentPostID != null (its a reply)
                            {
                                ReplyViewModel existingAuthorReply = ReplyViewModelList.Where(reply => reply.DiscussionPostId == dp.ID).FirstOrDefault();
                                if (existingAuthorReply == null)//adding replies
                                {
                                    ReplyViewModel reply = new ReplyViewModel();
                                    reply.Anonymize = reviewerAnonSetting;
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
                                    ReplyViewModelList[indexer].Anonymize = ReplyViewModelList[indexer].Anonymize || reviewerAnonSetting;
                                }
                            }
                            

                        }

                        foreach (DiscussionPost dp in NonTeamPosts)
                        {
                            if (dp.ParentPostID == null) //adding parent posts
                            {
                                DiscussionPostViewModel dpvm = new DiscussionPostViewModel();
                                dpvm.Anonymize = false; //These are instructors/tas/observers, etc. Do not anonymize, even if current user is moderator
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
                                dpvm.Anonymize = (assignment.DiscussionSettings.HasAnonymousPosts || AnonymizeForModerator) &&
                                                    (dp.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student); //Only anonymize students when it applies
                                dpvm.Content = dp.Content;
                                dpvm.CourseUser = dp.CourseUser;
                                dpvm.DiscussionPostId = dp.ID;
                                dpvm.Posted = dp.Posted;
                                DiscussionPostViewModelList.Add(dpvm);
                            }
                            else //adding replies
                            {
                                ReplyViewModel reply = new ReplyViewModel();
                                reply.Anonymize = (assignment.DiscussionSettings.HasAnonymousPosts || AnonymizeForModerator) &&
                                                    (dp.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student); //Only anonymize students when it applies;
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
                            dpvm.Anonymize = (assignment.DiscussionSettings.HasAnonymousPosts || AnonymizeForModerator) &&
                                                    (dp.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student); //Only anonymize students when it applies
                            dpvm.Content = dp.Content;
                            dpvm.CourseUser = dp.CourseUser;
                            dpvm.DiscussionPostId = dp.ID;
                            dpvm.Posted = dp.Posted;
                            DiscussionPostViewModelList.Add(dpvm);
                        }
                        else //adding replies
                        {
                            ReplyViewModel reply = new ReplyViewModel();
                            reply.Anonymize = (assignment.DiscussionSettings.HasAnonymousPosts || AnonymizeForModerator) &&
                                                (dp.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student); //Only anonymize students when it applies;
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

                setUpViewPermissionViewBag(discussionTeam, ViewBag.IsFirstPost);
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
        /// This function will set up viewbag variables related to viewing permissions for non-instructor/ta for a discussion assignment
        /// <param name="assignment"></param>
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="hasMadeFirstPost"></param>
        public void setUpViewPermissionViewBag(DiscussionTeam discussionTeam, bool hasMadeFirstPost)
        {
            Assignment assignment = discussionTeam.Assignment;
            bool isAnonymizedModerator = (assignment.DiscussionSettings.HasAnonymizationforModerators 
                    && (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator ));

            //Base values, determined by a combination of discussion settings, user role, and sent in parameters.
            ViewBag.CanSeeReplies = (assignment.DiscussionSettings.RequiresPostBeforeView == false) || hasMadeFirstPost;
            ViewBag.CanSeeAuthors = assignment.DiscussionSettings.HasAnonymousAuthors == false 
                                        && assignment.DiscussionSettings.HasAnonymousPosts == false 
                                        && isAnonymizedModerator == false;
            ViewBag.CanSeeReviewers = assignment.DiscussionSettings.HasAnonymousReviewers == false 
                                        && assignment.DiscussionSettings.HasAnonymousPosts == false 
                                        && isAnonymizedModerator == false;
        }

        /// <summary>
        /// This function will set up viewbag variables related to viewing permissions for an instructor/ta for a discussion assignment. 
        /// </summary>
        public void setUpInstructorViewingPermissionsViewBag()
        {
            //Instructors/TAs can view everything.
            ViewBag.CanSeeReplies = true;
            ViewBag.CanSeeAuthors = true;
            ViewBag.CanSeeReviewers = true;
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

                //Collecting posts and setting up DiscussionTeamList.
                if (assignment.HasDiscussionTeams)
                {
                    //Only want posts associated with discussionTeamID
                    ViewBag.DiscussionTeamList = assignment.DiscussionTeams.OrderBy(dt => dt.TeamName).ToList();

                    posts = (from post in db.DiscussionPosts
                                 .Include("Replies")
                                 where post.AssignmentID == assignment.ID &&
                                 post.DiscussionTeamID == discussionTeamID &&
                                 post.ParentPostID == null
                                 orderby post.Posted
                                 select post).ToList();
                }
                else
                {
                    //want all posts associated with assignmentId
                    posts = (from post in db.DiscussionPosts
                             .Include("Replies")
                             where post.AssignmentID == assignment.ID &&
                             post.ParentPostID == null
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


                setUpInstructorViewingPermissionsViewBag();
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
