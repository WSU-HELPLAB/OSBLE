using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using OSBLE.Models;

namespace OSBLE.Controllers
{
    public abstract class OSBLEController : Controller
    {
        private OSBLEContext db = new OSBLEContext();

        /// <summary>
        /// Provides common data for all controllers in the OSBLE app, such as profile information.
        /// </summary>
        public OSBLEController()
        {
            // If logged in, feed user profile to view.
            HttpContext context = System.Web.HttpContext.Current;

            if (context.User.Identity.IsAuthenticated)
            {
                ViewBag.UserProfile = db.UserProfiles.Where(u => u.UserName == context.User.Identity.Name).FirstOrDefault();
            }
        }
    }
}
