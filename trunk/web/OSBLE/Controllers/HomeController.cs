using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.WebPages;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;
using System.Collections;
using System.Net;

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

            #region Activity Feed View

            List<int> ViewedCourses = new List<int>();

            // Single course mode. Only display posts for the active course.
            if (DashboardSingleCourseMode)
            {
                ViewedCourses.Add(activeCourse.CourseID);
            }
            else // All course mode. Display posts for all non-hidden courses the user is attached to.
            {
                foreach (CoursesUsers cu in currentCourses)
                {
                    if (!cu.Hidden)
                    {
                        ViewedCourses.Add(cu.CourseID);
                    }
                }
            }

            // Pagination
            int startPost = 0;
            int postsPerPage = 10;
            if (Request.Params["startPost"] != null)
            {
                startPost = Convert.ToInt32(Request.Params["startPost"]);
            }

            // First get all posts to do a count and do proper paging
            IQueryable<DashboardPost> dashboardPosts = db.DashboardPosts.Where(d => ViewedCourses.Contains(d.CourseID)).OrderByDescending(d => d.Posted);
            ViewBag.DashboardPostCount = dashboardPosts.Count();
            
            // Only send items in page to the dashboard.
            ViewBag.DashboardPosts = dashboardPosts.Skip(startPost).Take(postsPerPage).ToList();
            ViewBag.StartPost = startPost;

            // Set up display settings for each post (recursively sets up replies.)
            foreach (DashboardPost dp in ViewBag.DashboardPosts)
            {
                setupPostDisplay(dp);
            }

            ViewBag.PostsPerPage = postsPerPage;

            #endregion Activity Feed View

            #region Notifications

            // Load all unread notifications for the current user to display on the dashboard.
            ViewBag.Notifications = db.Notifications.Where(n => (n.RecipientID == currentUser.ID) && (n.Read == false)).OrderByDescending(n => n.Posted).ToList();

            #endregion

            #region Events

            int eventDays = 7*ActiveCourse.Course.CalendarWindowOfTime;

            DateTime today = DateTime.Now.Date;
            DateTime upto = today.AddDays(eventDays);
            
            List<Event> events = ActiveCourse.Course.Events.Where(e => e.Approved && (e.StartDate >= today) && (e.StartDate <= upto)).ToList();

            // Add course meeting times and breaks.
            if (ActiveCourse.Course is Course && ((ActiveCourse.Course as Course).ShowMeetings == true))
            {
                Course course = (Course)ActiveCourse.Course;

                // Add breaks within window to events
                foreach (CourseBreak cb in course.CourseBreaks)
                {
                    // Start of break
                    if ((cb.StartDate >= today) && (cb.StartDate <= upto))
                    {
                        Event e = new Event();

                        e.Title = cb.Name;

                        if (cb.StartDate.Date != cb.EndDate.Date)
                        {
                            e.Title += " Starts";
                        }

                        e.StartDate = cb.StartDate.Date;
                        e.HideTime = true;
                        e.HideDelete = true;

                        events.Add(e);
                    }

                    // End of break (only if date is different than start)
                    if ((cb.StartDate.Date != cb.EndDate.Date) && (cb.EndDate >= today) && (cb.EndDate <= upto))
                    {
                        Event e = new Event();

                        e.Title = cb.Name + " Ends";
                        e.StartDate = cb.EndDate.Date;
                        e.HideTime = true;
                        e.HideDelete = true;
                        events.Add(e);
                    }
                }

                for (int day = 0; day < eventDays; day++)
                {
                    foreach (CourseMeeting cm in course.CourseMeetings)
                    {
                        DateTime current = today.AddDays(day);

                        // Wow, this is a big if statement.
                        if (
                            ((current.DayOfWeek == DayOfWeek.Sunday) && cm.Sunday) ||
                            ((current.DayOfWeek == DayOfWeek.Monday) && cm.Monday) ||
                            ((current.DayOfWeek == DayOfWeek.Tuesday) && cm.Tuesday) ||
                            ((current.DayOfWeek == DayOfWeek.Wednesday) && cm.Wednesday) ||
                            ((current.DayOfWeek == DayOfWeek.Thursday) && cm.Thursday) ||
                            ((current.DayOfWeek == DayOfWeek.Friday) && cm.Friday) ||
                            ((current.DayOfWeek == DayOfWeek.Saturday) && cm.Saturday)
                            )
                        {
                            Event e = new Event();

                            e.Title = cm.Name + " - " + cm.Location;
                            e.StartDate = current.AddHours((double)cm.StartTime.Hour).AddMinutes((double)cm.StartTime.Minute);
                            e.EndDate = current.AddHours((double)cm.EndTime.Hour).AddMinutes((double)cm.EndTime.Minute);
                            e.AllowLinking = true;
                            e.HideDelete = true;

                            // Do not show Course meetings outside of course start/end date and breaks.
                            if ((e.StartDate.Date >= course.StartDate.Date) && (e.StartDate.Date <= course.EndDate.Date) && (course.CourseBreaks.Where(b => (current >= b.StartDate) && (current <= b.EndDate)).Count() < 1))
                            {
                                events.Add(e);
                            }

                        }
                    }
                }
            }

            ViewBag.Events = events.OrderBy(e => e.StartDate);

            #endregion

            return View();
        }

        /// <summary>
        /// Sets up display settings for dashboard posts and replies.  
        /// </summary>
        /// <param name="post">The post or reply to be set up</param>
        /// <param name="courseList"></param>
        private void setupPostDisplay(AbstractDashboard post) {
            List<CoursesUsers> courseList = new List<CoursesUsers> ();

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
            if (!currentCu.CourseRole.Anonymized || (currentCu.UserProfileID == posterCu.UserProfileID) || posterCu.CourseRole.CanGrade)
            {
                // Display Name
                post.DisplayName = posterCu.UserProfile.FirstName + " " + posterCu.UserProfile.LastName;
                
                // Display Titles for Instructors/TAs for Courses, or Leader of Communities.
                if (posterCu.Course is Community)
                {
                    if (posterCu.CourseRoleID == (int)CommunityRole.OSBLERoles.Leader)
                    {
                        post.DisplayTitle = "Leader";
                    }
                }
                else if (posterCu.Course is Course)
                {
                    if (posterCu.CourseRoleID == (int)CourseRole.OSBLERoles.Instructor)
                    {
                        post.DisplayTitle = "Instructor";
                    }
                    else if (posterCu.CourseRoleID == (int)CourseRole.OSBLERoles.TA)
                    {
                        post.DisplayTitle = "TA";
                    }
                }

                // Allow deletion if current user is poster or is an instructor
                if ((posterCu.UserProfileID == currentCu.UserProfileID) || currentCu.CourseRole.CanModify)
                {
                    post.CanDelete = true;
                }

                // If current user is not the poster, allow mailing
                if (posterCu.UserProfileID != currentCu.UserProfileID)
                {
                    post.CanMail = true;
                }

                post.ShowProfilePicture = true;
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

            // For posts, set reply box display if the course allows replies or if Instructor/TA.
            if ( post is DashboardPost && 
                (
                (currentCu.Course is Course && 
                    ((currentCu.Course as Course).AllowDashboardReplies)
                     || (currentCu.CourseRole.CanGrade))
                // For communities, always allow replies
                || (currentCu.Course is Community)
                )
               )
            {
                (post as DashboardPost).CanReply = true;
            }
            else if (post is DashboardPost && currentCu.Course is Community) // Communities always allow replies.
            {
                (post as DashboardPost).CanReply = true;
            }

            // Also for posts only, recursively set the display for their replies.
            if (post is DashboardPost)
            {
                foreach (DashboardReply dr in (post as DashboardPost).Replies)
                {
                    setupPostDisplay(dr);
                }
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

            if (Request.Form["post_active"] != null)
            { // Post to active course only.
                CoursesToPost.Add(activeCourse);
            }
            else if (Request.Form["post_all"] != null)
            { // Post to all courses.
                CoursesToPost = currentCourses;
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
                if(cu.Course is Course) {
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
            bool show = false;
            UserProfile u = db.UserProfiles.Find(userProfile);

            if (userProfile == currentUser.ID)
            {
                show = true;
            }
            else
            {
                CoursesUsers ourCu = currentCourses.Where(c => c.CourseID == course).FirstOrDefault();
                CoursesUsers theirCu = db.CoursesUsers.Where(c => (c.CourseID == course) && (c.UserProfileID == u.ID)).FirstOrDefault();

                if ((ourCu != null) && (theirCu != null) && (!(ourCu.CourseRole.Anonymized) || (theirCu.CourseRole.CanGrade==true)))
                {
                    show = true;
                }
            }

            if (show == true)
            {
                return new FileStreamResult(FileSystem.GetProfilePictureOrDefault(u), "image/jpeg");
            }
            else
            {
                return new FileStreamResult(FileSystem.GetDefaultProfilePicture(), "image/jpeg");
            }
        }
    }
}