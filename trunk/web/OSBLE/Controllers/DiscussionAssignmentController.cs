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
                if (post.CourseUserID == ActiveCourse.ID)
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
                        if (tm.CourseUser.ID == ActiveCourse.ID)
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

            ViewBag.ActiveCourse = ActiveCourse;
            if (assignment.HasDiscussionTeams)
            {
                ViewBag.TeamName = "- " + discussionTeam.Team.Name;
            }
            else
            {
                ViewBag.TeamName = "";
            }
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
            AssignmentTeam at = new AssignmentTeam();
            List<AssignmentTeam> atList = new List<AssignmentTeam>();

            posts = (from post in db.DiscussionPosts
                     where post.AssignmentID == assignment.ID &&
                     !post.IsReply
                     orderby post.Posted
                     select post).ToList();

            Cache["PostOrReply"] = postOrReply;
            if (assignment != null && student != null && (postOrReply >= 0 && postOrReply <= 3))
            {
                Cache["StudentID"] = student.ID;

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
                            post.DisplayName = post.CourseUser.DisplayNameFirstLast(ActiveCourse.AbstractRole);
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
                        post.DisplayName = post.CourseUser.DisplayNameFirstLast(ActiveCourse.AbstractRole);
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
            

            ViewBag.Student = student;
            ViewBag.Assignment = assignment;
            ViewBag.FirstPost = true;
            ViewBag.ActiveCourse = ActiveCourse;
            ViewBag.PostOrReply = postOrReply;
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
            return View();
        }

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
                    post.DisplayName = post.CourseUser.DisplayNameFirstLast(ActiveCourse.AbstractRole);
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
                            post.DisplayName = post.CourseUser.DisplayNameFirstLast(ActiveCourse.AbstractRole);
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
                        post.DisplayName = post.CourseUser.DisplayNameFirstLast(ActiveCourse.AbstractRole);
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
            ViewBag.ActiveCourse = ActiveCourse;
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
        public ActionResult NewPost(DiscussionPost dp)
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
                            CourseUser = ActiveCourse,
                            CourseUserID = ActiveCourse.ID,
                            Posted = DateTime.Now,
                            AssignmentID = assignment.ID,
                            DisplayName = "Anonymous " + ActiveCourse.ID,
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
                            CourseUser = ActiveCourse,
                            CourseUserID = ActiveCourse.ID,
                            Posted = DateTime.Now,
                            AssignmentID = assignment.ID,
                            DisplayName = ActiveCourse.DisplayNameFirstLast(ActiveCourse.AbstractRole),
                            IsReply = false
                        };
                        db.DiscussionPosts.Add(post);
                        db.SaveChanges();
                    }
                }
            }
            if (ActiveCourse.AbstractRole.CanModify)
            {
                return RedirectToAction("TeacherIndex", new { assignmentId = assignment.ID, courseUserId = ActiveCourseUser.ID, postOrReply = (int)Cache["PostOrReply"] });
            }
            else if (ActiveCourse.AbstractRole.Anonymized)
            {
                return RedirectToAction("ObserverIndex", new { assignmentId = assignment.ID, courseUserId = ActiveCourseUser.ID, postOrReply = (int)Cache["PostOrReply"] });
            }
            else
            {
                return RedirectToAction("Index", new { assignmentId = assignment.ID });
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
        public ActionResult NewReply(DiscussionPost dr)
        {
            dr.CourseUserID = ActiveCourse.ID;
            dr.CourseUser = ActiveCourse;
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
                dr.DisplayName = dr.CourseUser.DisplayNameFirstLast(ActiveCourse.AbstractRole);
                dr.IsReply = true;
                replyToPost.Replies.Add(dr);

                AssignmentTeam at = GetAssignmentTeam(replyToPost.Assignment, ActiveCourse.UserProfile);

                db.SaveChanges();

                ViewBag.dp = replyToPost;
                List<DiscussionPost> replys = replyToPost.Replies.Where(r => r.ID > latestReply).ToList();

                ViewBag.DiscussionReplies = replys;
            }
            ViewBag.Assignment = dr.Assignment;
            ;
            if (ActiveCourse.AbstractRole.CanSubmit)
            {
                return RedirectToAction("Index", new { assignmentId = dr.AssignmentID });
            }
            else if (ActiveCourse.AbstractRole.CanModify)
            {
                int cuId = (int)Cache["StudentID"];
                int postOrReply = (int)Cache["PostOrReply"];
                return RedirectToAction("TeacherIndex", new { assignmentId = dr.AssignmentID, courseUserId = cuId, postOrReply = postOrReply });
            }
            else if (ActiveCourse.AbstractRole.Anonymized)
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