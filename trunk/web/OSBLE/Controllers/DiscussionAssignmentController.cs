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
            DiscussionTeam discussionTeam = new DiscussionTeam();
            List<DiscussionPost> posts = new List<DiscussionPost>();

            Assignment assignment = db.Assignments.Find(assignmentId);
            ViewBag.Assignment = assignment;
            ViewBag.Posts = null;
            ViewBag.FirstPost = false;

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
            
            //checking if user has made a first post
            if (!assignment.DiscussionSettings.RequiresPostBeforeView)
            {
                ViewBag.FirstPost = true;
            }
            else
            {
                foreach (DiscussionPost post in posts)
                {
                    if (post.CourseUserID == ActiveCourseUser.ID)
                    {
                        ViewBag.FirstPost = true;
                        break;
                    }
                }
            }
            // normal discussion: hasDiscussionTeams == true
            // Problem: CR disc: hasDiscussionTeams == false
            // Should ALWAYS have discussions teams?
            // Problem: discussionTeam is never initialized here
            ViewBag.Posts = posts.OrderBy(p => p.Posted);
            ViewBag.ActiveCourse = ActiveCourseUser;
            if (assignment.HasDiscussionTeams)
            {
                ViewBag.TeamName = "- " + discussionTeam.Team.Name;
            }
            else
            {
                ViewBag.TeamName = "";
            }
            ViewBag.DiscussionTeamID = discussionTeam.ID;
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="courseUserId"></param>
        /// <param name="postOrReply">postOrReply is used as enumerable. 0 = Posts, 1 = Replies, 2 = Both, 3 = No Selector</param>
        /// <param name="discussionTeamID"></param>
        /// <returns></returns>
        [CanModifyCourse]
        public ActionResult TeacherIndex(int assignmentId, int courseUserId, int postOrReply, int discussionTeamID)
        {
            List<DiscussionPost> teamPosts = new List<DiscussionPost>();
            List<DiscussionPost> posts = new List<DiscussionPost>();
            Assignment assignment = db.Assignments.Find(assignmentId);
            CourseUser student = db.CourseUsers.Find(courseUserId);
            DiscussionTeam discussionTeam = (from dt in assignment.DiscussionTeams
                                                 where dt.ID == discussionTeamID
                                                 select dt).FirstOrDefault();

            //Only filter by discussionTeamID if the assignment HasDiscussionTeams
            if (assignment.HasDiscussionTeams)
            {
                posts = (from post in db.DiscussionPosts
                         where post.AssignmentID == assignment.ID &&
                         post.DiscussionTeamID == discussionTeamID &&
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

            
            if (postOrReply == 3)  //If postOrReply is 3, we want no selections - so setting student to null.
           { 
                student = null;
            }    

            ViewBag.Student = student;
            ViewBag.Assignment = assignment;
            ViewBag.FirstPost = true;
            ViewBag.ActiveCourse = ActiveCourseUser;
            ViewBag.PostOrReply = postOrReply;
            ViewBag.TeamList = assignment.DiscussionTeams.OrderBy(s => s.Team.Name).ToList();
            if (assignment.HasDiscussionTeams) //Setting up TeamName and TeamList
            {
                ViewBag.TeamName = " - " + discussionTeam.Team.Name;
            }
            else
            {
                ViewBag.TeamName = " - " + student.DisplayNameFirstLast(ActiveCourseUser.AbstractRole);
            }
            
            ViewBag.TeamSelectId = "selected_team";
            ViewBag.AssignmentSelectId = "selected_assignment";
            if (discussionTeam.AssignmentID != 0)
            {
                ViewBag.SelectedTeam = discussionTeam;
            }
            else
            {
                ViewBag.SelectedTeam = null;
            }
            ViewBag.PostOrReply = postOrReply;
            ViewBag.DiscussionTeamID = discussionTeam.ID;
            return View();
        }

        [Obsolete("This function has not been kept up to date")]
        public ActionResult ObserverIndex(int assignmentId, int courseUserId, int postOrReply)
        {
            List<DiscussionPost> teamPosts = new List<DiscussionPost>();
            List<DiscussionPost> posts = new List<DiscussionPost>();
            Assignment assignment = db.Assignments.Find(assignmentId);
            CourseUser student = db.CourseUsers.Find(courseUserId);
            DiscussionTeam discussionTeam = new DiscussionTeam();

            

            posts = (from post in db.DiscussionPosts
                     where post.AssignmentID == assignment.ID &&
                     !post.IsReply
                     orderby post.Posted
                     select post).ToList();
            foreach (DiscussionPost post in posts)
            {
                if (!post.CourseUser.AbstractRole.CanModify)
                {
                    post.DisplayName = post.CourseUser.DisplayNameFirstLast(ActiveCourseUser.AbstractRole);
                }
            }

            Cache["PostOrReply"] = postOrReply;
            if (assignment != null && student != null && (postOrReply >= 0 && postOrReply <= 3))
            {
                Cache["StudentID"] = student.ID;
                if (assignment.HasDiscussionTeams)
                {
                    foreach (DiscussionTeam dt in assignment.DiscussionTeams)
                    {
                        foreach (TeamMember tm in dt.Team.TeamMembers)
                        {
                            if (tm.CourseUser.ID == student.ID)
                            {
                                discussionTeam = dt;
                                break;
                            }
                        }
                    }
                    foreach (TeamMember tm in discussionTeam.Team.TeamMembers)
                    {
                        posts = (from post in db.DiscussionPosts
                                 where post.AssignmentID == assignment.ID &&
                                 post.CourseUserID == tm.CourseUserID &&
                                 !post.IsReply
                                 select post).ToList();
                        foreach (DiscussionPost post in posts)
                        {
                            post.DisplayName = post.CourseUser.DisplayNameFirstLast(ActiveCourseUser.AbstractRole);
                            teamPosts.Add(post);
                        }
                    }
                    posts = (from post in db.DiscussionPosts
                             where post.AssignmentID == assignment.ID &&
                             post.CourseUser.AbstractRole.CanModify &&
                             !post.IsReply
                             select post).ToList();
                    foreach (DiscussionPost post in posts)
                    {
                        teamPosts.Add(post);
                    }
                    ViewBag.Posts = teamPosts.OrderBy(t => t.Posted);
                }

                else
                {
                    posts = (from post in db.DiscussionPosts
                             where post.AssignmentID == assignment.ID &&
                             !post.IsReply
                             orderby post.Posted
                             select post).ToList();
                    foreach (DiscussionPost post in posts)
                    {
                        post.DisplayName = post.CourseUser.DisplayNameFirstLast(ActiveCourseUser.AbstractRole);
                    }

                    foreach (AssignmentTeam a in assignment.AssignmentTeams)
                    {
                        if (a.Team.TeamMembers.FirstOrDefault().CourseUser.ID == student.ID)
                        {
                            discussionTeam.Assignment = a.Assignment;
                            discussionTeam.AssignmentID = a.AssignmentID;
                            discussionTeam.Team = a.Team;
                            discussionTeam.TeamID = a.TeamID;
                            break;
                        }
                    }
                    ViewBag.Posts = posts;
                }
            }

            if (postOrReply == 3)
            {
                if (!assignment.HasDiscussionTeams)
                {
                    ViewBag.Posts = posts;
                }
                student = null;
            }

            if (assignment.HasDiscussionTeams)
            {
                ViewBag.TeamName = " - " + discussionTeam.Team.Name;
                ViewBag.TeamList = assignment.DiscussionTeams.OrderBy(s => s.Team.Name).ToList();
            }
            else
            {
                if (student == null)
                {
                    ViewBag.TeamName = null;
                }
                else
                {
                    ViewBag.TeamName = " - " + student.DisplayNameFirstLast(ActiveCourse.AbstractRole);
                }

                List<DiscussionTeam> dtList = new List<DiscussionTeam>();
                DiscussionTeam dt = new DiscussionTeam();
                foreach (AssignmentTeam assignTeam in assignment.AssignmentTeams)
                {
                    dt.Assignment = assignTeam.Assignment;
                    dt.AssignmentID = assignTeam.AssignmentID;
                    dt.Team = assignTeam.Team;
                    dt.TeamID = assignTeam.TeamID;
                    dt.Team.Name = assignTeam.Team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.LastAndFirst();
                    dtList.Add(dt);

                    dt = new DiscussionTeam();
                }
                ViewBag.TeamList = dtList.OrderBy(l => l.Team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.LastName).ThenBy(f => f.Team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.FirstName).ToList();
            }

            ViewBag.Student = student;
            ViewBag.Assignment = assignment;
            ViewBag.FirstPost = true;
            ViewBag.ActiveCourse = ActiveCourseUser;
            ViewBag.PostOrReply = postOrReply;
            ViewBag.TeamSelectId = "selected_team";
            ViewBag.AssignmentSelectId = "selected_assignment";
            if (discussionTeam != null)
            {
                ViewBag.SelectedTeam = discussionTeam;
            }
            else
            {
                ViewBag.SelectedTeam = null;
            }
            return View("TeacherIndex");
        }

        [HttpPost]
        public ActionResult NewPost(DiscussionPost dp, int discussionTeamId)
        {
            Assignment assignment = db.Assignments.Find(dp.AssignmentID);
            if (dp.Content != null)
            {
                if (assignment != null)
                {
                    AssignmentTeam at = GetAssignmentTeam(assignment, ActiveCourse.UserProfile);
                    if (assignment.DiscussionSettings.HasAnonymousPosts)
                    {
                        DiscussionPost post = new DiscussionPost()
                        {
                            Content = dp.Content,
                            CourseUser = ActiveCourseUser,
                            CourseUserID = ActiveCourseUser.ID,
                            Posted = DateTime.Now,
                            AssignmentID = assignment.ID,
                            DisplayName = "Anonymous " + ActiveCourseUser.ID,
                            DiscussionTeamID = discussionTeamId,
                            IsReply = false
                        };
                        db.DiscussionPosts.Add(post);
                        db.SaveChanges();
                    }
                    else
                    {
                        DiscussionPost post = new DiscussionPost()
                        {
                            Content = dp.Content,
                            CourseUser = ActiveCourseUser,
                            CourseUserID = ActiveCourseUser.ID,
                            Posted = DateTime.Now,
                            AssignmentID = assignment.ID,
                            DisplayName = ActiveCourseUser.DisplayNameFirstLast(ActiveCourseUser.AbstractRole),
                            DiscussionTeamID = discussionTeamId,
                            IsReply = false
                        };
                        db.DiscussionPosts.Add(post);
                        db.SaveChanges();
                    }
                }
            }
            if (ActiveCourseUser.AbstractRole.CanModify)
            {
                return RedirectToAction("TeacherIndex", new { assignmentId = assignment.ID, courseUserId = ActiveCourseUser.ID, postOrReply = (int)Cache["PostOrReply"], discussionTeamId = discussionTeamId });
            }
            else if (ActiveCourseUser.AbstractRole.Anonymized)
            {
                return RedirectToAction("ObserverIndex", new { assignmentId = assignment.ID, courseUserId = ActiveCourseUser.ID, postOrReply = (int)Cache["PostOrReply"] });
            }
            else
            {
                return RedirectToAction("Index", new { assignmentId = assignment.ID, discussionTeamId = discussionTeamId });
            }
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
        public ActionResult NewReply(DiscussionPost dr, int discussionTeamId)
        {
            dr.CourseUserID = ActiveCourseUser.ID;
            dr.CourseUser = ActiveCourseUser;
            dr.Posted = DateTime.Now;

            int replyTo = 0;
            if (Request.Form["reply_to"] != null)
            {
                replyTo = Convert.ToInt32(Request.Form["reply_to"]);
            }

            int latestReply = 0;
            if (Request.Form["latest_reply"] != null)
            {
                latestReply = Convert.ToInt32(Request.Form["latest_reply"]);
            }

            DiscussionPost replyToPost = db.DiscussionPosts.Find(replyTo);
            if (replyToPost != null)
            {
                dr.AssignmentID = replyToPost.AssignmentID;
                dr.DisplayName = dr.CourseUser.DisplayNameFirstLast(ActiveCourseUser.AbstractRole);
                dr.IsReply = true;
                dr.DiscussionTeamID = discussionTeamId;
                replyToPost.Replies.Add(dr);

                AssignmentTeam at = GetAssignmentTeam(replyToPost.Assignment, ActiveCourseUser.UserProfile);

                db.SaveChanges();

                ViewBag.dp = replyToPost;
                List<DiscussionPost> replys = replyToPost.Replies.Where(r => r.ID > latestReply).ToList();

                ViewBag.DiscussionReplies = replys;
            }
            ViewBag.Assignment = dr.Assignment;
            if (ActiveCourseUser.AbstractRole.CanSubmit)
            {
                return RedirectToAction("Index", new { assignmentId = dr.AssignmentID, discussionTeamId = discussionTeamId });
            }
            else if (ActiveCourseUser.AbstractRole.CanModify)
            {
                int cuId = (int)Cache["StudentID"];
                int postOrReply = (int)Cache["PostOrReply"];
                return RedirectToAction("TeacherIndex", new { assignmentId = dr.AssignmentID, courseUserId = cuId, postOrReply = postOrReply, discussionTeamId = discussionTeamId });
            }
            else if (ActiveCourseUser.AbstractRole.Anonymized)
            {
                int cuId = (int)Cache["StudentID"];
                int postOrReply = (int)Cache["PostOrReply"];
                return RedirectToAction("ObserverIndex", new { assignmentId = dr.AssignmentID, courseUserId = cuId, postOrReply = postOrReply });
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}