using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Attributes;
using System.IO;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;

namespace OSBLE.Controllers
{
    public class DiscussionAssignmentController : OSBLEController
    {
        //
        // GET: /DiscussionAssignment/

        public ActionResult Index(int assignmentId)
        {
            List<DiscussionPost> teamPosts = new List<DiscussionPost>();
            DiscussionTeam discussionTeam = new DiscussionTeam();
            Assignment assignment = db.Assignments.Find(assignmentId);
            ViewBag.Assignment = assignment;
            ViewBag.Posts = null;
            ViewBag.FirstPost = false;
            List<DiscussionPost> posts = (from post in db.DiscussionPosts
                                          where post.AssignmentID == assignment.ID
                                          orderby post.Posted
                                          select post).ToList();

            foreach (DiscussionPost post in posts)
            {
                if (post.CourseUserID == activeCourse.ID)
                {
                    ViewBag.FirstPost = true;
                }
            }
                
            if (assignment.HasDiscussionTeams)
            {
                foreach (DiscussionTeam dt in assignment.DiscussionTeams)
                {
                    foreach (TeamMember tm in dt.Team.TeamMembers)
                    {
                        if (tm.CourseUser.ID == activeCourse.ID)
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
                                post.CourseUserID == tm.CourseUserID
                                select post).ToList();
                    foreach (DiscussionPost post in posts)
                    {
                        teamPosts.Add(post);
                    }
                    db.SaveChanges();
                }
                ViewBag.Posts = teamPosts;
            }
            else
            {
                ViewBag.Posts = posts;
            }

            ViewBag.ActiveCourse = activeCourse;
            return View();
        }

        public ActionResult TeacherIndex(int assignmentId, int courseUserId)
        {
            List<DiscussionPost> teamPosts = new List<DiscussionPost>();
            List<DiscussionPost> posts = new List<DiscussionPost>();
            Assignment assignment = db.Assignments.Find(assignmentId);
            CourseUser student = db.CourseUsers.Find(courseUserId);
            DiscussionTeam discussionTeam = new DiscussionTeam();

            Session["StudentID"] = student.ID;
            
            if (assignment != null && student != null)
            {
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
                                 post.CourseUserID == tm.CourseUserID
                                 select post).ToList();
                        foreach (DiscussionPost post in posts)
                        {
                            teamPosts.Add(post);
                        }
                        db.SaveChanges();
                    }
                    ViewBag.Posts = teamPosts;
                }
                else
                {
                    posts = (from post in db.DiscussionPosts
                             where post.AssignmentID == assignment.ID
                             orderby post.Posted
                             select post).ToList();
                    ViewBag.Posts = posts;
                }
            }
            ViewBag.Student = student;
            ViewBag.Assignment = assignment;
            ViewBag.FirstPost = true;
            ViewBag.ActiveCourse = activeCourse;
            return View();
        }

        [HttpPost]
        public ActionResult NewPost(DiscussionPost dp)
        {
            Assignment assignment = db.Assignments.Find(dp.AssignmentID);
            if (dp.Content != null)
            {
                if (assignment != null)
                {
                    if (assignment.DiscussionSettings.HasAnonymousPosts)
                    {
                        DiscussionPost post = new DiscussionPost()
                        {
                            Content = dp.Content,
                            CourseUser = activeCourse,
                            CourseUserID = activeCourse.ID,
                            Posted = DateTime.Now,
                            AssignmentID = assignment.ID,
                            DisplayName = "Anonymous"
                        };
                        db.DiscussionPosts.Add(post);
                        db.SaveChanges();
                    }
                    else
                    {
                        DiscussionPost post = new DiscussionPost()
                        {
                            Content = dp.Content,
                            CourseUser = activeCourse,
                            CourseUserID = activeCourse.ID,
                            Posted = DateTime.Now,
                            AssignmentID = assignment.ID,
                            DisplayName = activeCourse.UserProfile.FirstName + " " + activeCourse.UserProfile.LastName
                        };
                        db.DiscussionPosts.Add(post);
                        db.SaveChanges();
                    }
                }
            }
            return RedirectToAction("Index", new { assignmentId = assignment.ID });

        }
        [HttpGet, FileCache(Duration = 3600)]
        public FileStreamResult ProfilePictureForDashboard(int course, int userProfile)
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
            if (userProfile == currentUser.ID ||
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
        public ActionResult NewReply(DiscussionReply dr)
        {
            dr.CourseUserID = activeCourse.ID;
            dr.CourseUser = activeCourse;
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
                dr.DisplayName = activeCourse.UserProfile.FirstName + " " + activeCourse.UserProfile.LastName;
                replyToPost.Replies.Add(dr);
                db.SaveChanges();

                ViewBag.dp = replyToPost;
                List<DiscussionReply> replys = replyToPost.Replies.Where(r => r.ID > latestReply).ToList();

                ViewBag.DiscussionReplies = replys;
            }
            ViewBag.Assignment = dr.Assignment;
            if (activeCourse.AbstractRole.CanSubmit)
            {
                return RedirectToAction("Index", new { assignmentId = dr.AssignmentID });
            }
            else if (activeCourse.AbstractRole.CanModify)
            {
                int cuId = (int)Session["StudentID"];
                return RedirectToAction("TeacherIndex", new { assignmentId = dr.AssignmentID, courseUserId = cuId });
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
