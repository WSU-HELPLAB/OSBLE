﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Users;
using OSBLE.Utility;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
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

            //changed to load partial view
            //setupActivityFeed(); // Dashboard posts/replies            

            setupNotifications(); // Individual notifications (mail, grades, etc.)        

            //changed to load partial view
            //setupEvents(); // Events & Deadlines

            setupCourseLinks(); // Quickly accessible course files

            return View("Index");
        }

        /// <summary>
        /// Views a particular dashboard post thread by itself.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult ViewThread(int id)
        {
            DashboardPost dp = db.DashboardPosts.Find(id);
            // Ensure post exists and that user is in the course.
            if (dp == null || (currentCourses.Where(cc => cc.AbstractCourseID == dp.CourseUser.AbstractCourseID).Count() < 1))
            {
                return RedirectToAction("Index");
            }

            setupPostDisplay(dp);

            ViewBag.DashboardPost = dp;

            ViewBag.DashboardSingleCourseMode = false; // Force course/community prefixes to show when viewing thread.

            return View(dp);
        }

        public ActionResult ActivityFeed()
        {
            setupActivityFeed();
            return PartialView("_ActivityFeed");
        }

        private void setupActivityFeed()
        {
            // Pagination defaults
            int startPost = 0;
            int postsPerPage = 10;

            // Single course mode. Only display posts for the active course.
            List<int> viewedCourses = DashboardSingleCourseMode ? new List<int> {ActiveCourseUser.AbstractCourseID} : currentCourses.Where(cu => !cu.Hidden).Select(cu => cu.AbstractCourseID).ToList();

            if (ActiveCourseUser.AbstractCourse is Course && ActiveCourseUser.AbstractRole.CanModify)
            {
                ViewBag.IsInstructor = true;
            }
            else
            {
                ViewBag.IsInstructor = false;
            }

            if (ActiveCourseUser.AbstractCourse is Community && ActiveCourseUser.AbstractRole.CanModify)
            {
                ViewBag.IsLeader = true;
            }
            else
            {
                ViewBag.IsLeader = false;
            }

            // Get optional start post from query for pagination
            if (Request.Params["startPost"] != null)
            {
                startPost = Convert.ToInt32(Request.Params["startPost"]);
            }

            // First get all posts to do a count and then do proper paging
            List<DashboardPost> dashboardPosts = db.DashboardPosts.Where(d => viewedCourses.Contains(d.CourseUser.AbstractCourseID))
                .OrderByDescending(d => d.Posted).ToList();
            ViewBag.ShowAll = false;
            if (Request.Params["showAll"] != null)
            {
                bool showAll = Convert.ToBoolean(Request.Params["showAll"]);
                ViewBag.ShowAll = showAll;
                if (showAll)
                {
                    postsPerPage = dashboardPosts.Count();
                }
            }
            
            List<DashboardPost> pagedDashboardPosts = dashboardPosts.Skip(startPost)
                .Take(postsPerPage)
                .ToList();

            // Set up display settings for each post (recursively sets up replies.)
            foreach (DashboardPost dp in pagedDashboardPosts)
            {
                setupPostDisplay(dp);
            }

            // Getting the maximum comment length for an activity feed
            getMaxCommentLength();

            // Only send items in page to the dashboard.
            ViewBag.DashboardPostCount = dashboardPosts.Count();
            ViewBag.DashboardPosts = pagedDashboardPosts;
            ViewBag.StartPost = startPost;
            ViewBag.PostsPerPage = postsPerPage;
        }

        private void setupCourseLinks()
        {
            DirectoryListing listing = FileSystem.GetCourseDocumentsFileList(ActiveCourseUser.AbstractCourse, false);
            SilverlightObject fileUploader = new SilverlightObject
            {
                CSSId = "file_uploader",
                XapName = "FileUploader",
                Width = "45",
                Height = "25",
                OnLoaded = "SLObjectLoaded",
                Parameters = new Dictionary<string, string>()
                {
                }
            };

            //AC: I don't think that this is the best way to restrict access.  Might be worth
            //revisiting at a later date.
            ViewBag.CanEditCourseLinks = false;
            if (ActiveCourseUser.AbstractRole.CanModify)
            {
                ViewBag.CanEditCourseLinks = true;
            }
            ViewBag.CourseLinks = listing;
            ViewBag.Uploader = fileUploader;            
        }

        public ActionResult Notifications()
        {
            setupNotifications();
            return PartialView("_ConsolidatedNotifications");
        }

        private void setupNotifications()
        {
            // Load all unread notifications for the current user to display on the dashboard.
            ViewBag.Notifications = db.Notifications.Where(n => (n.RecipientID == ActiveCourseUser.ID) && (n.Read == false)).OrderByDescending(n => n.Posted).ToList();
        }

        public ActionResult Events()
        {
            setupEvents();
           
            return PartialView("_Events", (List<Event>)ViewBag.Events);
        }

        private void setupEvents()
        {
            // Set start and end dates of event viewing to current viewing settings for the course
            int eventDays = 7 * ActiveCourseUser.AbstractCourse.CalendarWindowOfTime;

            DateTime today = DateTime.Now.Date;
            
            DateTime upto = today.AddDays(eventDays);
            using (EventController ec = new EventController())
            {
                ViewBag.Events = ec.GetActiveCourseEvents(today, upto);
            }
        }

        /// <summary>
        /// Sets up display settings for dashboard posts and replies.
        /// </summary>
        /// <param name="post">The post or reply to be set up</param>
        /// <param name="courseList"></param>
        private void setupPostDisplay(AbstractDashboard post)
        {
            List<CourseUser> courseList = new List<CourseUser>();

            // Get list of users in course, either from the root post or its parent in the case of a reply.
            if (post is DashboardPost)
            {
                DashboardPost dp = post as DashboardPost;
                courseList = db.CourseUsers.Where(c => c.AbstractCourseID == dp.CourseUser.AbstractCourseID).ToList();
            }
            else if (post is DashboardReply)
            {
                DashboardReply dr = post as DashboardReply;
                courseList = db.CourseUsers.Where(c => c.AbstractCourseID == dr.Parent.CourseUser.AbstractCourseID).ToList();
            }

            // Get Course/User link for current user.
            CourseUser currentCu = courseList.Where(c => c.UserProfileID == CurrentUser.ID).FirstOrDefault();

            // " " for poster of post/reply.
            CourseUser posterCu = courseList.Where(c => c.UserProfileID == post.CourseUser.UserProfileID).FirstOrDefault();

            // Setup Display Name/Display Title/Profile Picture/Mail Button/Delete Button

            // If user is not anonymous, this post was written by current user, or the poster is an Instructor/TA, display name and picture.
            if (currentCu != null && ((posterCu == null) || !currentCu.AbstractRole.Anonymized || (currentCu.UserProfileID == posterCu.UserProfileID) || posterCu.AbstractRole.CanGrade))
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
                if (currentCu != null && (currentCu.AbstractRole.CanModify || ((posterCu != null) && (posterCu.UserProfileID == currentCu.UserProfileID))))
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

                    post.DisplayTitle = getRoleTitle(posterCu.AbstractRoleID);

                    post.ShowProfilePicture = true;
                }
            }
            else // Display anonymous name.
            {
                // Anonymous number is currently the number of the student in the course list.
                post.DisplayName = posterCu.DisplayName(ActiveCourseUser.AbstractRole);

                // Profile picture will display default picture.
                post.ShowProfilePicture = false;
                post.CanMail = false;
                post.CanDelete = false;
            }

            // For root posts only
            if (post is DashboardPost)
            {
                DashboardPost thisDp = post as DashboardPost;

                // For posts, set reply box display if the course allows replies or if Instructor/TA/Observer.
                if (currentCu != null && ((currentCu.AbstractCourse is Course &&
                                           ((currentCu.AbstractCourse as Course).AllowDashboardReplies)
                                           || (currentCu.AbstractRole.CanGrade)
                                           || (currentCu.AbstractRole.Anonymized))
                    // For communities, always allow replies
                                          || (currentCu.AbstractCourse is Community))
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
        /// <param name="AbstractRoleID">The Role ID of the user in question</param>
        /// <returns>Returns a title for a user in a course if they are in a leadership role in that course</returns>
        private static string getRoleTitle(int AbstractRoleID)
        {
            switch (AbstractRoleID)
            {
                case (int)CommunityRole.OSBLERoles.Leader:
                    return "Leader";
                case (int)CourseRole.CourseRoles.Instructor:
                    return "Instructor";
                case (int)CourseRole.CourseRoles.TA:
                    return "TA";
                default:
                    return "";
            }
        }

        public ActionResult NoCourses()
        {
            if (ActiveCourseUser != null)
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
        /// Same as SetCourse() below, but gets the course from the GET instead of
        /// the POST.  Created to allow auto-navigating to specific dashboard posts.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string Course(int courseId, int? postId)
        {
            Cache["ActiveCourse"] = courseId;
            string url = string.Format("/#dp_post_{0}", postId);
            Response.Redirect(url);
            return "Redirecting...";
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
                    int courseId = 0;
                    if (Int32.TryParse(Request.Form["course"].ToString(), out courseId))
                    {
                        SetCourseID(courseId);
                    }
                    else
                    {
                        return RedirectToAction("Index"); 
                    }
                }
                catch (System.FormatException)
                {
                    // Non-integer entered. Ignore and redirect to root.
                    return RedirectToAction("Index"); 
                }
            }

            if (Request.Form["redirect"] != null)
            {
                //return Redirect(Request.Form["redirect"]);
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Sets the course for the current user
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public bool SetCourseID(int courseId)
        {
            try
            {
                Cache["ActiveCourse"] = courseId;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
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
                    Cache["DashboardSingleCourseMode"] = Convert.ToBoolean(Request.Form["mode"]);
                }
                catch (System.FormatException)
                {
                    // Non-integer input. Default to false.
                    Cache["DashboardSingleCourseMode"] = true;
                }
            }

            // Return to Dashboard.
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Gets the maximum Activity Feed comment length
        /// </summary>
        /// <returns></returns>
        public void getMaxCommentLength()
        {
            ViewBag.MaxActivityFeedLength = 4000;
        }

        [HttpPost]
        public ActionResult NewPost(DashboardPost dp)
        {
            dp.CourseUser = ActiveCourseUser;
            dp.Posted = DateTime.UtcNow;

            List<CourseUser> CoursesToPost = new List<CourseUser>();

            bool sendEmail = Convert.ToBoolean(Request.Form["send_email"]);

            if (Request.Form["post_active"] != null)
            { // Post to active course only.
                CoursesToPost.Add(ActiveCourseUser);
            }
            else if (Request.Form["post_all"] != null)
            { // Post to all courses.
                CoursesToPost = currentCourses.Where(cu => cu.AbstractCourse is Course && cu.AbstractRole.CanModify && !cu.Hidden).ToList();
            }

            foreach (CourseUser cu in CoursesToPost)
            {
                AbstractCourse c = null;
                if (cu.AbstractCourse is AbstractCourse)
                {
                    c = (AbstractCourse)cu.AbstractCourse;
                }
                if (cu.AbstractRole.CanGrade || ((c != null) && (c.AllowDashboardPosts)))
                {
                    DashboardPost newDp = new DashboardPost();
                    newDp.Content = dp.Content;
                    newDp.Posted = dp.Posted;
                    newDp.CourseUser = dp.CourseUser;

                    if (ModelState.IsValid)
                    {
                        db.DashboardPosts.Add(newDp);
                        db.SaveChanges();

                        //construct the subject & body
                        string subject = "";
                        string body = "";
                        List<MailAddress> addresses = new List<MailAddress>();

                        //dp.posted needs to have a converetd time for mailing
                        //locate timezone offset
                        int courseOffset = (ActiveCourseUser.AbstractCourse).GetType() != typeof(Community) ? ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset : 0;
                        CourseController cc = new CourseController();
                        TimeZoneInfo tz = cc.getTimeZone(courseOffset);

                        //slightly different messages depending on course type
                        if (c is Course)
                        {
                            Course course = c as Course;
                            subject = "[" + course.Prefix + " " + course.Number + "] New Activity Post from " + CurrentUser.FirstName + " " + CurrentUser.LastName;
                            body = CurrentUser.FirstName + " " + CurrentUser.LastName + " sent the following message to the class at " + TimeZoneInfo.ConvertTimeFromUtc(dp.Posted, tz).ToString() + ":";
                        }
                        else if (c is Community)
                        {
                            Community community = c as Community;
                            subject = "[" + community.Nickname + "] New Activity Post from " + CurrentUser.FirstName + " " + CurrentUser.LastName;
                            body = CurrentUser.FirstName + " " + CurrentUser.LastName + " sent the following message to the community at " + TimeZoneInfo.ConvertTimeFromUtc(dp.Posted, tz).ToString() + ":";
                        }
                        else
                        {
                            //this should never execute, but just in case...
                            subject = "OSBLE Activity Post";
                            body = CurrentUser.FirstName + " " + CurrentUser.LastName + " sent the following message at " + TimeZoneInfo.ConvertTimeFromUtc(dp.Posted, tz).ToString() + ":";
                        }
                        body += "<br /><br />";
                        body += dp.Content.Replace("\n", "<br />");
                        body += string.Format("<br /><br /><a href=\"http://osble.org/Home/Course?courseId={0}&postId={1}\">View and reply to post in OSBLE</a>",
                            newDp.CourseUser.AbstractCourseID,
                            newDp.ID
                            );

                        List<CourseUser> CourseUser = db.CourseUsers.Where(couseuser => couseuser.AbstractCourseID == c.ID).ToList();

                        //Who gets this email?  If the instructor desires, we send to everyone
                        if (c != null && sendEmail && cu.AbstractRole.CanModify)
                        {
                            foreach (CourseUser member in CourseUser)
                            {
                                if (member.UserProfile.UserName != null) // Ignore pending users
                                {
                                    addresses.Add(new MailAddress(member.UserProfile.UserName, member.UserProfile.FirstName + " " + member.UserProfile.LastName));
                                }
                            }
                        }
                        //If the instructor didn't want to send to everyone, only send to those
                        //that want to receive everything
                        else
                        {
                            foreach (CourseUser member in CourseUser)
                            {
                                if (member.UserProfile.UserName != null && member.UserProfile.EmailAllActivityPosts) // Ignore pending users
                                {
                                    addresses.Add(new MailAddress(member.UserProfile.UserName, member.UserProfile.FirstName + " " + member.UserProfile.LastName));
                                }
                            }
                        }

                        //Send the message
                        Email.Send(subject, body, addresses);
                    }
                }
            }

            db.SaveChanges();
            return RedirectToAction("Index");            
        }

        [HttpPost]
        public ActionResult NewReply(DashboardReply dr)
        {
            if (ModelState.IsValid)
            {
                dr.CourseUser = ActiveCourseUser;
                dr.Posted = DateTime.UtcNow;

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
                    CourseUser cu = (from c in currentCourses
                                       where c.AbstractCourseID == replyToPost.CourseUser.AbstractCourseID
                                       select c).FirstOrDefault();

                    AbstractCourse ac = null;
                    if (cu != null && cu.AbstractCourse is AbstractCourse)
                    {
                        ac = (AbstractCourse)cu.AbstractCourse;
                    }
                    if ((cu != null) && (cu.AbstractRole.CanGrade || ((ac != null) && (ac.AllowDashboardPosts))))
                    {
                        replyToPost.Replies.Add(dr);
                        db.SaveChanges();

                        //construct the subject & body
                        string subject = "";
                        string body = "";
                        List<MailAddress> addresses = new List<MailAddress>();

                        ViewBag.dp = replyToPost;
                        List<DashboardReply> replys = replyToPost.Replies.Where(r => r.ID > latestReply).ToList();

                        //dr.posted needs to have a converetd time for mailing
                        //locate timezone offset
                        int courseOffset = (ActiveCourseUser.AbstractCourse).GetType() != typeof(Community) ? ((Course)ActiveCourseUser.AbstractCourse).TimeZoneOffset : 0;
                        CourseController cc = new CourseController();
                        TimeZoneInfo tz = cc.getTimeZone(courseOffset);

                        //slightly different messages depending on course type
                        if (ac is Course && (ac as Course).AllowDashboardReplies)
                        {
                            Course course = (Course)ac;
                            subject = "[" + course.Prefix + " " + course.Number + "] Reply from " + CurrentUser.FirstName + " " + CurrentUser.LastName;
                            body = CurrentUser.FirstName + " " + CurrentUser.LastName + " sent the following reply to the Dashboard post " + replyToPost.DisplayTitle + " at " + TimeZoneInfo.ConvertTimeFromUtc(dr.Posted, tz).ToString() + ":";
                        }
                        else if (ac is Community)
                        {
                            Community community = ac as Community;
                            subject = "[" + community.Nickname + "] Reply from " + CurrentUser.FirstName + " " + CurrentUser.LastName;
                            body = CurrentUser.FirstName + " " + CurrentUser.LastName + " sent the following reply to the Dashboard post " + replyToPost.DisplayTitle + " at " + TimeZoneInfo.ConvertTimeFromUtc(dr.Posted, tz).ToString() + ":";
                        }
                        else
                        {
                            //this should never execute, but just in case...
                            subject = "OSBLE Activity Post";
                            body = CurrentUser.FirstName + " " + CurrentUser.LastName + " sent the following message at " + TimeZoneInfo.ConvertTimeFromUtc(dr.Posted, tz).ToString() + ":";
                        }
                        body += "<br /><br />";
                        body += dr.Content.Replace("\n", "<br />");
                        body += string.Format("<br /><br /><a href=\"http://osble.org/Home/Course?courseId={0}&postId={1}\">View and reply to post in OSBLE</a>",
                            dr.CourseUser.AbstractCourseID,
                            dr.ID
                            );

                        //List<CoursesUsers> courseUsers = db.CoursesUsers.Where(c => (c.AbstractCourseID == ac.ID && c.UserProfile.EmailAllActivityPosts)).ToList();
                        List<CourseUser> courseUsers = (from c in db.CourseUsers
                                                          where c.AbstractCourseID == ac.ID &&
                                                          c.UserProfile.EmailAllActivityPosts &&
                                                          c.UserProfileID != CurrentUser.ID
                                                          select c).ToList();

                        foreach (CourseUser member in courseUsers)
                        {
                            if (member.UserProfile.UserName != null) // Ignore pending users
                            {
                                addresses.Add(new MailAddress(member.UserProfile.UserName, member.UserProfile.FirstName + " " + member.UserProfile.LastName));
                            }
                        }

                        //Send the message
                        Email.Send(subject, body, addresses);

                        foreach (DashboardReply r in replys)
                        {
                            setupPostDisplay(r);
                        }

                        replys.Clear();
                        replys.Add(dr);
                        ViewBag.DashboardReplies = replys;

                        // Post notification to other thread participants
                        using (NotificationController nc = new NotificationController())
                        {
                            nc.SendDashboardNotification(dr.Parent, dr.CourseUser);
                        }
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
                CourseUser cu = currentCourses.FirstOrDefault(c => c.AbstractCourseID == dp.CourseUser.AbstractCourseID);
                if ((dp.CourseUserID == ActiveCourseUser.ID) || ((cu != null) && (cu.AbstractRole.CanGrade)))
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
                CourseUser cu = currentCourses.FirstOrDefault(c => c.AbstractCourse == dr.Parent.CourseUser.AbstractCourse);
                if ((dr.CourseUserID == ActiveCourseUser.ID) || ((cu != null) && (cu.AbstractRole.CanGrade)))
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

        [OsbleAuthorize]
        public ActionResult Time()
        {
            if (CurrentUser != null)
            {
                UserProfile user = db.UserProfiles.Find(CurrentUser.ID);
                if (user != null)
                {
                    return View();
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return RedirectToAction("Index");
            }
        }
    }
}