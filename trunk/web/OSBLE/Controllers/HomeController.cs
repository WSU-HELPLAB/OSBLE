using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.HomePage;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models;
using System.Drawing;
using OSBLE.Models.Services.Uploader;
using System.IO;
using System.Net.Mail;
using System.Configuration;

namespace OSBLE.Controllers
{
    [Authorize]
    public class HomeController : OSBLEController
    {
        /// <summary>
        /// Main action for the OSBLE Dashboard
        /// </summary>
        /// <returns></returns>
        ///
        [RequireActiveCourse]
        public ActionResult Index()
        {
            ViewBag.CurrentTab = "Dashboard";

            setupActivityFeed(); // Dashboard posts/replies

            setupNotifications(); // Individual notifications (mail, grades, etc.)

            setupEvents(); // Events & Deadlines

            setupCourseLinks(); // Quickly accessible course files

            return View();
        }

        /// <summary>
        /// Views a particular dashboard post thread by itself.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult ViewThread(int id)
        {
            DashboardPost dp = db.DashboardPosts.Find(id);
            if (dp == null || (currentCourses.Where(cc => cc.CourseID == dp.CourseID).Count() < 1))
            {
                return RedirectToAction("Index");
            }

            setupPostDisplay(dp);

            ViewBag.DashboardPost = dp;

            return View(dp);
        }

        private void setupActivityFeed()
        {
            List<int> viewedCourses;
            List<DashboardPost> dashboardPosts;
            List<DashboardPost> pagedDashboardPosts;

            // Pagination defaults
            int startPost = 0;
            int postsPerPage = 10;

            // Single course mode. Only display posts for the active course.
            if (DashboardSingleCourseMode)
            {
                viewedCourses = new List<int>();
                viewedCourses.Add(activeCourse.CourseID);
            }
            else // All course mode. Display posts for all non-hidden courses the user is attached to.
            {
                viewedCourses = currentCourses.Where(cu => !cu.Hidden).Select(cu => cu.CourseID).ToList();
            }

            
            if (activeCourse.Course is Course && activeCourse.CourseRole.CanModify)
            {
                ViewBag.IsInstructor = true;
            }
            else
            {
                ViewBag.IsInstructor = false;
            }

            // Get optional start post from query for pagination
            if (Request.Params["startPost"] != null)
            {
                startPost = Convert.ToInt32(Request.Params["startPost"]);
            }

            // First get all posts to do a count and then do proper paging
            dashboardPosts = db.DashboardPosts.Where(d => viewedCourses.Contains(d.CourseID))
                                                                        .OrderByDescending(d => d.Posted).ToList();

            pagedDashboardPosts = dashboardPosts.Skip(startPost)
                                                .Take(postsPerPage)
                                                .ToList();

            // Set up display settings for each post (recursively sets up replies.)
            foreach (DashboardPost dp in pagedDashboardPosts)
            {
                setupPostDisplay(dp);
            }

            // Only send items in page to the dashboard.
            ViewBag.DashboardPostCount = dashboardPosts.Count();
            ViewBag.DashboardPosts = pagedDashboardPosts;
            ViewBag.StartPost = startPost;
            ViewBag.PostsPerPage = postsPerPage;
        }

        private void setupCourseLinks()
        {
            DirectoryListing listing = FileSystem.GetCourseDocumentsFileList(activeCourse.Course, false);
            ViewBag.CourseLinks = listing;
        }

        private void setupNotifications()
        {
            // Load all unread notifications for the current user to display on the dashboard.
            ViewBag.Notifications = db.Notifications.Where(n => (n.RecipientID == currentUser.ID) && (n.Read == false)).OrderByDescending(n => n.Posted).ToList();
        }

        private void setupEvents()
        {
            // Set start and end dates of event viewing to current viewing settings for the course
            int eventDays = 7 * ActiveCourse.Course.CalendarWindowOfTime;

            DateTime today = DateTime.Now.Date;
            DateTime upto = today.AddDays(eventDays);

            EventController ec = new EventController();
            ViewBag.Events = ec.GetActiveCourseEvents(today, upto);
        }

        /// <summary>
        /// Sets up display settings for dashboard posts and replies.
        /// </summary>
        /// <param name="post">The post or reply to be set up</param>
        /// <param name="courseList"></param>
        private void setupPostDisplay(AbstractDashboard post)
        {
            List<CoursesUsers> courseList = new List<CoursesUsers>();

            // Get list of users in course, either from the root post or its parent in the case of a reply.
            if (post is DashboardPost)
            {
                DashboardPost dp = post as DashboardPost;
                courseList = db.CoursesUsers.Where(c => c.CourseID == dp.CourseID).ToList();
            }
            else if (post is DashboardReply)
            {
                DashboardReply dr = post as DashboardReply;
                courseList = db.CoursesUsers.Where(c => c.CourseID == dr.Parent.CourseID).ToList();
            }

            // Get Course/User link for current user.
            CoursesUsers currentCu = courseList.Where(c => c.UserProfileID == currentUser.ID).FirstOrDefault();

            // " " for poster of post/reply.
            CoursesUsers posterCu = courseList.Where(c => c.UserProfileID == post.UserProfileID).FirstOrDefault();

            // Setup Display Name/Display Title/Profile Picture/Mail Button/Delete Button

            // If user is not anonymous, this post was written by current user, or the poster is an Instructor/TA, display name and picture.
            if ((posterCu == null) || !currentCu.CourseRole.Anonymized || (currentCu.UserProfileID == posterCu.UserProfileID) || posterCu.CourseRole.CanGrade)
            {
                // Display Name
                if (posterCu != null)
                {
                    post.DisplayName = posterCu.UserProfile.FirstName + " " + posterCu.UserProfile.LastName;
                }
                else
                {
                    post.DisplayName = "Deleted User";
                }

                // Allow deletion if current user is poster or is an instructor
                if (currentCu.CourseRole.CanModify || ((posterCu != null) && (posterCu.UserProfileID == currentCu.UserProfileID)))
                {
                    post.CanDelete = true;
                }

                // If current user is not the poster, allow mailing
                if (posterCu != null && posterCu.UserProfileID != currentCu.UserProfileID)
                {
                    post.CanMail = true;
                }

                if (posterCu != null)
                {
                    // Display Titles for Instructors/TAs for Courses, or Leader of Communities.

                    post.DisplayTitle = getRoleTitle(posterCu.CourseRoleID);

                    post.ShowProfilePicture = true;
                }
            }
            else // Display anonymous name.
            {
                // Anonymous number is currently the number of the student in the course list.
                // TODO: Investigate better anonymous numbering?
                post.DisplayName = "Anonymous " + courseList.IndexOf(posterCu);

                // Profile picture will display default picture.
                post.ShowProfilePicture = false;
                post.CanMail = false;
                post.CanDelete = false;
            }

            // For root posts only
            if (post is DashboardPost)
            {
                DashboardPost thisDp = post as DashboardPost;

                // For posts, set reply box display if the course allows replies or if Instructor/TA.
                if ((currentCu.Course is Course &&
                        ((currentCu.Course as Course).AllowDashboardReplies)
                         || (currentCu.CourseRole.CanGrade))
                    // For communities, always allow replies
                    || (currentCu.Course is Community)
                    )
                {
                    thisDp.CanReply = true;
                }

                // recursively set the display for post's replies.
                foreach (DashboardReply dr in thisDp.Replies)
                {
                    setupPostDisplay(dr);
                }
            }
        }

        /// <summary>
        /// Gets optional role title for certain roles in a course/community
        /// (Instructors/TAs in courses, Leaders in communities)
        /// </summary>
        /// <param name="CourseRoleID">The Role ID of the user in question</param>
        /// <returns>Returns a title for a user in a course if they are in a leadership role in that course</returns>
        private string getRoleTitle(int CourseRoleID)
        {
            switch (CourseRoleID)
            {
                case (int)CommunityRole.OSBLERoles.Leader:
                    return "Leader";
                case (int)CourseRole.OSBLERoles.Instructor:
                    return "Instructor";
                case (int)CourseRole.OSBLERoles.TA:
                    return "TA";
                default:
                    return "";
            }
        }

        public ActionResult NoCourses()
        {
            if (ActiveCourse != null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CurrentTab = "Dashboard";

            return View();
        }

        public ActionResult NoAccess()
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Sets active course and redirects back to where we came from.
        /// </summary>
        /// <returns></returns>
        public ActionResult SetCourse()
        {
            // Sets active course and redirects back to where we came from.
            if (Request.Form["course"] != null)
            {
                try
                {
                    context.Session["ActiveCourse"] = Convert.ToInt32(Request.Form["course"]);
                }
                catch (System.FormatException)
                {
                    // Non-integer entered. Ignore and redirect to root.
                    return Redirect("/");
                }
            }

            if (Request.Form["redirect"] != null)
            {
                return Redirect(Request.Form["redirect"]);
            }
            else
            {
                return Redirect("/");
            }
        }

        /// <summary>
        /// Sets "All courses" or "Active course" setting
        /// </summary>
        /// <returns></returns>
        public ActionResult SetDashboardMode()
        {
            if (Request.Form["mode"] != null)
            {
                try
                {
                    context.Session["DashboardSingleCourseMode"] = Convert.ToBoolean(Request.Form["mode"]);
                }
                catch (System.FormatException)
                {
                    // Non-integer input. Default to false.
                    context.Session["DashboardSingleCourseMode"] = false;
                }
            }

            // Return to Dashboard.
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult NewPost(DashboardPost dp)
        {
            dp.UserProfile = currentUser;
            dp.Posted = DateTime.Now;

            List<CoursesUsers> CoursesToPost = new List<CoursesUsers>();

            bool sendEmail = Convert.ToBoolean(Request.Form["send_email"]);

            if (Request.Form["post_active"] != null)
            { // Post to active course only.
                CoursesToPost.Add(activeCourse);
            }
            else if (Request.Form["post_all"] != null)
            { // Post to all courses.
                CoursesToPost = currentCourses.Where(cu=>cu.Course is Course && cu.CourseRole.CanModify && !cu.Hidden).ToList();
            }

            foreach (CoursesUsers cu in CoursesToPost)
            {
                Course c = null;
                if (cu.Course is Course)
                {
                    c = (Course)cu.Course;
                }
                if (cu.CourseRole.CanGrade || ((c != null) && (c.AllowDashboardPosts)))
                {
                    DashboardPost newDp = new DashboardPost();
                    newDp.Content = dp.Content;
                    newDp.Posted = dp.Posted;
                    newDp.UserProfile = dp.UserProfile;
                    newDp.Course = cu.Course;

                    if (ModelState.IsValid)
                    {
                        db.DashboardPosts.Add(newDp);

#if !DEBUG
                        // Instructor email to class
                        if (c != null && sendEmail && cu.CourseRole.CanModify)
                        {
                            // Construct email
                            SmtpClient mailClient = new SmtpClient();
                            mailClient.UseDefaultCredentials = true;

                            MailAddress toFrom = new MailAddress(ConfigurationManager.AppSettings["OSBLEFromEmail"],"OSBLE");
                            MailMessage mm = new MailMessage();

                            mm.From = toFrom;
                            mm.To.Add(toFrom);

                            mm.Subject = "[OSBLE " + c.Prefix + " " + c.Number + "] Notice from Instructor " + currentUser.FirstName + " " + currentUser.LastName;

                            mm.Body = currentUser.FirstName + " " + currentUser.LastName + " sent the following message to the class at " + dp.Posted.ToString() + ":";
                            mm.Body += "\n\n";
                            mm.Body += dp.Content;

                            foreach (CoursesUsers member in db.CoursesUsers.Where(coursesUsers => coursesUsers.CourseID == c.ID).ToList())
                            {
                                if (member.UserProfile.UserName != null) // Ignore pending users
                                {
                                    mm.Bcc.Add(new MailAddress(member.UserProfile.UserName, member.UserProfile.FirstName + " " + member.UserProfile.LastName));
                                }
                            }

                            mailClient.Send(mm);
                        }
#endif
                    }
                }
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult NewReply(DashboardReply dr)
        {
            dr.UserProfile = currentUser;
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

            DashboardPost replyToPost = db.DashboardPosts.Find(replyTo);
            if (replyToPost != null)
            { // Does the post we're replying to exist?
                // Are we a member of the course we're replying to?
                CoursesUsers cu = (from c in currentCourses
                                   where c.CourseID == replyToPost.CourseID
                                   select c).FirstOrDefault();

                Course course = null;
                if (cu.Course is Course)
                {
                    course = (Course)cu.Course;
                }
                if ((cu != null) && (cu.CourseRole.CanGrade || ((course != null) && (course.AllowDashboardReplies))))
                {
                    if (ModelState.IsValid)
                    {
                        replyToPost.Replies.Add(dr);
                        db.SaveChanges();
                    }
                }
                else
                {
                    Response.StatusCode = 403;
                }

                ViewBag.dp = replyToPost;
                List<DashboardReply> replys = replyToPost.Replies.Where(r => r.ID > latestReply).ToList();

                foreach (DashboardReply r in replys)
                {
                    setupPostDisplay(r);
                }

                ViewBag.DashboardReplies = replys;
            }
            else
            {
                Response.StatusCode = 403;
            }

            return View("_SubDashboardReply");
        }

        /// <summary>
        /// Removes a Dashboard Post, AJAX-style!
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ///
        [HttpPost]
        public ActionResult DeletePost(int id)
        {
            DashboardPost dp = db.DashboardPosts.Find(id);

            if (dp != null)
            {
                CoursesUsers cu = currentCourses.Where(c => c.Course == dp.Course).FirstOrDefault();
                if ((dp.UserProfileID == currentUser.ID) || ((cu != null) && (cu.CourseRole.CanGrade)))
                {
                    dp.Replies.Clear();
                    db.SaveChanges();
                    db.DashboardPosts.Remove(dp);
                    db.SaveChanges();
                }
                else
                {
                    Response.StatusCode = 403;
                }
            }
            else
            {
                Response.StatusCode = 403;
            }

            return View("_AjaxEmpty");
        }

        /// <summary>
        /// Removes a Reply, AJAX-style!
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DeleteReply(int id)
        {
            DashboardReply dr = db.DashboardReplies.Find(id);

            if (dr != null)
            {
                CoursesUsers cu = currentCourses.Where(c => c.Course == dr.Parent.Course).FirstOrDefault();
                if ((dr.UserProfileID == currentUser.ID) || ((cu != null) && (cu.CourseRole.CanGrade)))
                {
                    db.DashboardReplies.Remove(dr);
                    db.SaveChanges();
                }
                else
                {
                    Response.StatusCode = 403;
                }
            }
            else
            {
                Response.StatusCode = 403;
            }
            return View("_AjaxEmpty");
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
            AbstractRole ourRole = currentCourses.Where(c => c.CourseID == course).Select(c=>c.CourseRole).FirstOrDefault();
            AbstractRole theirRole = db.CoursesUsers.Where(c => (c.CourseID == course) && (c.UserProfileID == userProfile)).Select(c=>c.CourseRole).FirstOrDefault();

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
    }
}