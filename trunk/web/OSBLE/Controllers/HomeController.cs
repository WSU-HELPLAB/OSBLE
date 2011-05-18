using System;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;
using System.Collections.Generic;
using System.Linq;

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

            // Validate dashboard display mode setting.
            if ((context.Session["DashboardSingleCourseMode"] == null) || (context.Session["DashboardSingleCourseMode"].GetType() != typeof(Boolean)))
            {
                context.Session["DashboardSingleCourseMode"] = false;
            }

            bool DashboardSingleCourseMode = ViewBag.DashboardSingleCourseMode = context.Session["DashboardSingleCourseMode"];

            #region Activity Feed Posting
            // Feed Post attempt
            if (Request.Form["post_message"] != null)
            {
                // TODO: Allow courses to allow students to post to first level of feed.
                int replyTo = 0;
                if (Request.Form["reply_to"] != null)
                {
                    replyTo = Convert.ToInt32(Request.Form["reply_to"]);
                }
                
                DashboardPost dp = new DashboardPost();
                dp.Content = Request.Form["post_content"];
                dp.UserProfile = currentUser;

                if(replyTo != 0) {
                    DashboardPost replyToPost = db.DashboardPosts.Find(replyTo); 
                    if(replyToPost != null) { // Does the post we're replying to exist?
                        // Are we a member of the course we're replying to?
                        CoursesUsers cu = (from c in currentCourses
                                          where c.CourseID == replyToPost.CourseID
                                          select c).FirstOrDefault();

                        DashboardReply dr = new DashboardReply();
                        dr.Content = dp.Content;
                        dr.UserProfile = dp.UserProfile;
                        
                        if(cu != null) {
                            replyToPost.Replies.Add(dr);
                        }                               
                                                    
                    }
                } else {
                    List<CoursesUsers> CoursesToPost = new List<CoursesUsers>();

                    if(Request.Form["post_active"] != null) { // Post to active course only.
                        CoursesToPost.Add(activeCourse);
                    } else if(Request.Form["post_all"] != null) { // Post to all courses.
                        CoursesToPost = currentCourses;
                    }

                    foreach (CoursesUsers cu in CoursesToPost)
                    {
                        if (cu.CourseRole.CanGrade) // TODO: Add course permissions for student posting
                        {
                            DashboardPost newDp = new DashboardPost();
                            newDp.Content = dp.Content;
                            newDp.Posted = dp.Posted;
                            newDp.UserProfile = dp.UserProfile;
                            newDp.Course = cu.Course;

                            db.DashboardPosts.Add(newDp);
                        }
                    }
                }

                db.SaveChanges();
                return RedirectToAction("Index"); // Redirect so we don't have refresh data
            }

            #endregion

            #region Activity Feed View

            List<int> ViewedCourses = new List<int>();

            if(DashboardSingleCourseMode) {
                ViewedCourses.Add(activeCourse.CourseID);
            } else {
                foreach(CoursesUsers cu in currentCourses) {
                    ViewedCourses.Add(cu.CourseID);
                }
            }

            // TODO: Add Pagination
            ViewBag.DashboardPosts = db.DashboardPosts.Where(d => ViewedCourses.Contains(d.CourseID)).OrderByDescending(d => d.Posted).ToList();

            #endregion

            return View();
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

        /// <summary>
        /// Removes a Dashboard Post
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Removes a Reply
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
            }
            return RedirectToAction("Index");
        }
    }
}