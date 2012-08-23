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
        public bool AnonymizeNameForDiscussion(CourseUser poster, DiscussionSetting discussionSetting)
        {
            bool Anonymous = false;
            if (poster.ID != ActiveCourseUser.ID)   //Don't want to set anonymous permissions if the poster is the current user, so users own posts arent anonymous
            {

                int postersUserRoleId = poster.AbstractRoleID;
                int currentUserRoleId = ActiveCourseUser.AbstractRoleID;

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
        public bool AnonymizeNameForCriticalReviewDiscussion(CourseUser poster, 
            Assignment assignment, bool posterIsAuthor, bool posterIsReviewer, bool currentUserIsAuthor, bool currentUserIsReviewer)
        {
            bool Anonymous = false;

            CriticalReviewSettings crSettings = assignment.PreceedingAssignment.CriticalReviewSettings;
            if(crSettings.AnonymizeAuthor && currentUserIsReviewer && posterIsAuthor)
            {
                Anonymous = true;
            }
            else if(crSettings.AnonymizeComments && currentUserIsAuthor && posterIsReviewer)
            {
                Anonymous = true;
            }
            //Purposefully not handling crSettings.AnonymizeReviewerToReviewers as this is property is used for the reviewed document not discussions. There is another
            //setting in place to anonmize reviewers to reviewers in discussion settins.


            //Anonymize if Anonymous is true or AnonymizeNameForDiscussion (anonymize based on discussion settings) is true.
            return (Anonymous || AnonymizeNameForDiscussion(poster, assignment.DiscussionSettings));
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
                DiscussionViewModel dvm = new DiscussionViewModel(discussionTeam, ActiveCourseUser);


                //Checking if its users first post
                ViewBag.IsFirstPost = (from dpvm in dvm.DiscussionPostViewModels
                                       where dpvm.poster.CourseUser.ID == ActiveCourseUser.ID
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
                //additionally for CRD assignments we want to display all teammates invovled in the discussion
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

                ViewBag.CanPost = assignment.DueDate > DateTime.Now;
                ViewBag.DiscussionPostViewModelList = dvm.DiscussionPostViewModels.OrderBy(dpvm => dpvm.Posted).ToList();
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
        /// <param name="discussionTeamID">The discussion team id for discussion to be viewed. If it is a classwide discussion, then any dt in that assignment can be sent.</param>
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

                DiscussionViewModel dvm = new DiscussionViewModel(discussionTeam, ActiveCourseUser);


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

                bool canPost = true;
                //If the user is a TA and TAs can only participate in some discussions, then we must confirm the TA
                //is in the team before we give them permission to post
                if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA && !assignment.DiscussionSettings.TAsCanPostToAllDiscussions
                    && discussionTeam.Team.TeamMembers.Where(tm => tm.CourseUserID == ActiveCourseUser.ID).ToList().Count == 0)
                {
                    canPost = false;
                }

                //for CRD assignment types we need a list of all discussions they can participate in for navigation.
                //additionally for CRD assignments we want to display all teammates invovled in the discussion
                if (assignment.HasDiscussionTeams)
                {
                    ViewBag.DiscussionTeamList = assignment.DiscussionTeams;

                }

                ViewBag.CanPost = canPost;
                ViewBag.DiscussionPostViewModelList = dvm.DiscussionPostViewModels;
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
