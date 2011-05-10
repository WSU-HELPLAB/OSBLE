﻿using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    [SessionState(SessionStateBehavior.Default)]
    public abstract class OSBLEController : Controller
    {
        protected OSBLEContext db = new OSBLEContext();
        protected UserProfile currentUser = null;
        protected CoursesUsers activeCourse = null;
        protected HttpContext context = System.Web.HttpContext.Current;
        protected List<CoursesUsers> currentCourses = new List<CoursesUsers>();

        /// <summary>
        /// Provides common data for all controllers in the OSBLE app, such as profile and current course information.
        /// </summary>
        public OSBLEController()
        {
            // If logged in, feed user profile to view.

            if (context.User.Identity.IsAuthenticated)
            {
                #region User And Course Setup

                // Set current User Profile.
                string userName = context.User.Identity.Name;
                ViewBag.UserProfile = currentUser = db.UserProfiles.Where(u => u.UserName == userName).FirstOrDefault();

                // Get list of enrolled courses.
                ViewBag.CurrentCourses = currentCourses = db.CoursesUsers.Where(cu => cu.UserProfileID == currentUser.ID).ToList();

                // First we need to validate that the Active Course session variable is actually an integer.
                // We could just cast it, but the only place this gets assigned is here and in the Home/SetCourse action,
                // And they should only set it to be an integer. So, if it's anything else, it was probably unauthorized and
                // we should null the value.
                if (context.Session["ActiveCourse"] != null && context.Session["ActiveCourse"].GetType() != typeof(int))
                {
                    context.Session["ActiveCourse"] = 0;
                }

                // Load currently selected course. Ensure user is actually a member of that course.
                if ((context.Session["ActiveCourse"] != null) && (currentCourses.Where(cu => cu.CourseID == (int)context.Session["ActiveCourse"]).Count() > 0))
                {
                    activeCourse = currentCourses.Where(cu => cu.CourseID == (int)context.Session["ActiveCourse"]).First();
                }
                else if (currentCourses.Count() > 0) // If no active course, assign to first in courses list.
                {
                    activeCourse = currentCourses.First();
                    context.Session["ActiveCourse"] = activeCourse.CourseID;
                }
                else // No courses!
                {
                    context.Session["ActiveCourse"] = null;
                }

                ViewBag.ActiveCourse = activeCourse;

                #endregion User And Course Setup
            }
        }
    }
}