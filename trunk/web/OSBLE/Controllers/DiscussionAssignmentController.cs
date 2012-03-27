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
            List<DiscussionPost> posts = new List<DiscussionPost>();
            Assignment assignment = db.Assignments.Find(assignmentId);
            ViewBag.Assignment = assignment;
            ViewBag.Posts = null;
            ViewBag.FirstPost = false;

            if (!assignment.DiscussionSettings.RequiresPostBeforeView)
            {
                ViewBag.FirstPost = true; 
            }

            posts = (from post in db.DiscussionPosts
                     where post.AssignmentID == assignment.ID &&
                     !post.IsReply
                     orderby post.Posted
                     select post).ToList();

            foreach (DiscussionPost post in posts)
            {
                if (post.CourseUserID == activeCourse.ID)
                {
                    ViewBag.FirstPost = true;
                    break;
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
                             post.CourseUserID == tm.CourseUserID &&
                             !post.IsReply
                             select post).ToList();

                    foreach (DiscussionPost post in posts)
                    {
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
                ViewBag.Posts = posts;
            }

            ViewBag.ActiveCourse = activeCourse;
            return View();
        }

        [CanModifyCourse]
        public ActionResult TeacherIndex(int assignmentId, int courseUserId, int postOrReply)
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

            Session["PostOrReply"] = postOrReply;
            if (assignment != null && student != null && (postOrReply >= 0 && postOrReply <= 3))
            {
                Session["StudentID"] = student.ID;
                
                if (assignment.HasDiscussionTeams)
                {
                    foreach (DiscussionTeam dt in assignment.DiscussionTeams)
                    {
                        List<TeamMember> tmList = dt.Team.TeamMembers.OrderBy(s => s.CourseUser.UserProfile.LastName).ThenBy(r => r.CourseUser.UserProfile.FirstName).ToList();
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

            ViewBag.Student = student;
            ViewBag.Assignment = assignment;
            ViewBag.FirstPost = true;
            ViewBag.ActiveCourse = activeCourse;
            ViewBag.PostOrReply = postOrReply;
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
                    AssignmentTeam at = GetAssignmentTeam(assignment, activeCourse.UserProfile);
                    if (assignment.DiscussionSettings.HasAnonymousPosts)
                    {
                        List<CourseUser> users = GetAnonymizedCourseUserList(activeCourse.AbstractCourseID);
                        int i = -1;
                        foreach (CourseUser u in users)
                        {
                            i++;
                            if (u.ID == activeCourse.ID)
                            {
                                break;
                            }
                        }
                        DiscussionPost post = new DiscussionPost()
                        {
                            Content = dp.Content,
                            CourseUser = activeCourse,
                            CourseUserID = activeCourse.ID,
                            Posted = DateTime.Now,
                            AssignmentID = assignment.ID,
                            DisplayName = "Anonymous " + i.ToString(),
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
                            CourseUser = activeCourse,
                            CourseUserID = activeCourse.ID,
                            Posted = DateTime.Now,
                            AssignmentID = assignment.ID,
                            DisplayName = activeCourse.UserProfile.FirstName + " " + activeCourse.UserProfile.LastName,
                            IsReply = false
                        };
                        db.DiscussionPosts.Add(post);
                        db.SaveChanges();
                    }
                    
                }
            }
            if (activeCourse.AbstractRole.CanModify)
            {
                return RedirectToAction("TeacherIndex", new { assignmentId = assignment.ID, courseUserId = (int)Session["StudentId"], postOrReply = (int)Session["PostOrReply"]});
            }
            else
            {
                return RedirectToAction("Index", new { assignmentId = assignment.ID });
            }

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
        public ActionResult NewReply(DiscussionPost dr)
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
                dr.IsReply = true;
                replyToPost.Replies.Add(dr);

                AssignmentTeam at = GetAssignmentTeam(replyToPost.Assignment, activeCourse.UserProfile);

                db.SaveChanges();

                ViewBag.dp = replyToPost;
                List<DiscussionPost> replys = replyToPost.Replies.Where(r => r.ID > latestReply).ToList();

                ViewBag.DiscussionReplies = replys;
            }
            ViewBag.Assignment = dr.Assignment;
            ;
            if (activeCourse.AbstractRole.CanSubmit)
            {
                return RedirectToAction("Index", new { assignmentId = dr.AssignmentID });
            }
            else if (activeCourse.AbstractRole.CanModify)
            {
                int cuId = (int)Session["StudentID"];
                int postOrReply = (int)Session["PostOrReply"];
                return RedirectToAction("TeacherIndex", new { assignmentId = dr.AssignmentID, courseUserId = cuId, postOrReply = postOrReply });
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
