using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using OSBLE.Models;
using System.Web.Security;

namespace OSBLE.Controllers
{
    [SessionState(SessionStateBehavior.Default)]
    public abstract class OSBLEController : Controller
    {
        protected OSBLEContext db = new OSBLEContext();
        protected UserProfile currentUser = null;

        public UserProfile CurrentUser
        {
            get { return currentUser; }
        }

        protected CoursesUsers activeCourse = null;

        public CoursesUsers ActiveCourse
        {
            get { return activeCourse; }
        }

        protected HttpContext context = System.Web.HttpContext.Current;
        protected List<CoursesUsers> currentCourses = new List<CoursesUsers>();

        /// <summary>
        /// Defines a menu item tab
        /// </summary>
        public class MenuItem
        {
            public string Name { get; set; }
            public string Controller { get; set; }
            public string Action { get; set; }

            public bool ModifierOnly { get; set; }
            public bool ViewerOnly { get; set; }
            public bool AdminOnly { get; set; }
            
            /// <summary>
            /// Creates a menu item that everyone can access.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="controller"></param>
            /// <param name="action"></param>
            public MenuItem(string name, string controller, string action)
            {
                this.Name = name;
                this.Controller = controller;
                this.Action = action;

                this.ModifierOnly = false;
                this.ViewerOnly = false;
                this.AdminOnly = false;
            }

            /// <summary>
            /// Creates a menu item with particular privileges set for its display.
            /// NOTE: This only affects the display of the menu item. You still need to use attributes to validate pages!
            /// </summary>
            /// <param name="name"></param>
            /// <param name="controller"></param>
            /// <param name="action"></param>
            /// <param name="modifierOnly"></param>
            /// <param name="viewerOnly"></param>
            /// <param name="adminOnly"></param>
            public MenuItem(string name, string controller, string action, bool modifierOnly, bool viewerOnly, bool adminOnly)
            {
                this.Name = name;
                this.Controller = controller;
                this.Action = action;

                this.ModifierOnly = modifierOnly;
                this.ViewerOnly = viewerOnly;
                this.AdminOnly = adminOnly;
            }

        }

        /// <summary>
        /// Provides common data for all controllers in the OSBLE app, such as profile and current course information.
        /// </summary>
        public OSBLEController()
        {
            // If logged in, feed user profile to view.

            if (context.User.Identity.IsAuthenticated)
            {
                #region Menu Setup

                List<MenuItem> menu = new List<MenuItem>();

                menu.Add(new MenuItem("Dashboard", "Home", "Index"));
                menu.Add(new MenuItem("About", "Home", "About"));
                menu.Add(new MenuItem("Users", "Roster", "Index", true, true, false));
                ViewBag.Menu = menu;

                #endregion Menu Setup

                #region User And Course Setup

                // Set current User Profile.
                string userName = context.User.Identity.Name;
                ViewBag.CurrentUser = currentUser = db.UserProfiles.Where(u => u.UserName == userName).FirstOrDefault();

                // Get list of enrolled courses.
                if (currentUser != null)
                {
                    // Get list of courses this user is connected to.
                    currentCourses = db.CoursesUsers.Where(cu => cu.UserProfileID == currentUser.ID).ToList();

                    // First we need to validate that the Active Course session variable is actually an integer.
                    if (context.Session["ActiveCourse"] != null && context.Session["ActiveCourse"].GetType() != typeof(int))
                    {
                        context.Session["ActiveCourse"] = null;
                    }

                    // Load currently selected course, as long as user is actually a member of said course.
                    if ((context.Session["ActiveCourse"] != null) && (currentCourses.Where(cu => cu.CourseID == (int)context.Session["ActiveCourse"]).Count() > 0))
                    {
                        activeCourse = currentCourses.Where(cu => cu.CourseID == (int)context.Session["ActiveCourse"]).First();
                        context.Session["ActiveCourse"] = activeCourse.CourseID;
                    }
                    else // Assign first course if one exists.
                    {
                        activeCourse = currentCourses.FirstOrDefault();
                        if (activeCourse != null)
                        {
                            context.Session["ActiveCourse"] = activeCourse.CourseID;
                        }
                        else
                        {
                            context.Session["ActiveCourse"] = null;
                        }
                    }

                    ViewBag.ActiveCourse = activeCourse;
                }
                else // User invalid. Logout.
                {
                    context.Session.Clear(); // Clear session
                    FormsAuthentication.SignOut();
                }

                ViewBag.CurrentCourses = currentCourses;

                #endregion User And Course Setup
            }
        }
    }
}