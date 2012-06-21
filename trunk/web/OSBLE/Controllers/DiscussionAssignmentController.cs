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

            //if ActiveCourseUser belongs to the discussionTeam, continue. Otherwise kick them out!!
            if(isInDiscussionTeam)
            {
                List<DiscussionPost> posts;
                //Only filter by discussionTeamID if the assignment HasDiscussionTeams
                if (assignment.HasDiscussionTeams)
                {
                    posts = (from post in db.DiscussionPosts
                             where post.AssignmentID == assignment.ID &&
                             post.DiscussionTeamID == discussionTeamId &&
                             !post.IsReply
                             orderby post.Posted
                             select post).ToList();
                }
                else
                {
                    posts = (from post in db.DiscussionPosts
                             where post.AssignmentID == assignment.ID &&
                             !post.IsReply
                             orderby post.Posted
                             select post).ToList();
                }

                    

                ViewBag.IsFirstPost = true;
                //Marking IsFirstPost as false if any posts are found that belong to that user.
                foreach (DiscussionPost post in posts)
                {
                    if (post.CourseUserID == ActiveCourseUser.ID)
                    {
                        ViewBag.IsFirstPost = false;
                        break;
                    }
                }

                //assigning TeamName a value if teams exist.
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

                ViewBag.Posts = posts.OrderBy(p => p.Posted);
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
        [CanModifyCourse]
        public ActionResult TeacherIndex(int assignmentId, int discussionTeamID, int courseUserId = 0, int postOrReply = 3)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
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
                             where post.AssignmentID == assignment.ID &&
                             post.DiscussionTeamID == discussionTeamID &&
                             !post.IsReply
                             orderby post.Posted
                             select post).ToList();
                }
                else
                {
                    //want all posts associated with assignmentId
                    posts = (from post in db.DiscussionPosts
                             where post.AssignmentID == assignment.ID &&
                             !post.IsReply
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

        [HttpPost]
        public ActionResult NewReply(DiscussionPost reply)
        {
            int ParentPostID = 0;
            if (Request.Form["ParentPostID"] != null)
            {
                ParentPostID = Convert.ToInt32(Request.Form["ParentPostID"]);
            }
            
            DiscussionPost replyToPost = db.DiscussionPosts.Find(ParentPostID);
            if (replyToPost != null && reply.Content != null && reply.ParentPostID > 0 && reply.DiscussionTeamID > 0 && reply.AssignmentID > 0)
            {
                Assignment assignment = db.Assignments.Find(reply.AssignmentID);
                reply.CourseUserID = ActiveCourseUser.ID;
                reply.IsReply = true;
                
                replyToPost.Replies.Add(reply);
                db.SaveChanges();
            }
            return Redirect(Request.UrlReferrer.ToString());
        }
    }
}
