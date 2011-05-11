using System;
using System.Web.Mvc;

namespace OSBLE.Controllers
{
    public class HomeController : OSBLEController
    {
        /// <summary>
        /// Main action for the OSBLE Dashboard
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult Index()
        {
            ViewBag.CurrentTab = "Dashboard";

            if(activeCourse == null) {
                return RedirectToAction("NoCourses");
            }

            // Validate dashboard display mode setting.
            if ((context.Session["DashboardSingleCourseMode"] == null) || (context.Session["DashboardMode"].GetType() != typeof(Boolean)))
            {
                context.Session["DashboardSingleCourseMode"] = false;
            }

            ViewBag.DashboardSingleCourseMode = context.Session["DashboardSingleCourseMode"];

            return View();
        }

        [Authorize]
        public ActionResult NoCourses()
        {
            if (activeCourse != null)
            {
                return RedirectToAction("Index");           
            }

            ViewBag.CurrentTab = "Dashboard";

            return View(); 
        }

        [Authorize]
        public ActionResult About()
        {
            ViewBag.CurrentTab = "About";

            return View();
        }

        /// <summary>
        /// Sets active course and redirects back to where we came from.
        /// </summary>
        /// <returns></returns>
        [Authorize]
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
        [Authorize]
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
            return Redirect("/");
        }
    }
}