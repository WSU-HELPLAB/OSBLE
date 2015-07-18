﻿namespace OSBLE.Services
{
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
    using System.Web;
    using OSBLE.Models;
    using OSBLE.Models.Courses;
    using OSBLE.Models.Users;
    using OSBLE.Utility;

    /// <summary>
    /// Provides current user and active course context,
    /// as well as database access
    /// for services that inherit from it.
    /// </summary>
    [EnableClientAccess()]

    //Took this out in hopes that DomainService would work
    //[RequiresAuthentication()]
    public class OSBLEService : DomainService
    {
        protected OSBLEContext db = new OSBLEContext();
        protected CourseUser currentCourseUser = null;
        protected UserProfile currentUserProfile = null;
        protected AbstractCourse currentCourse = null;

        protected HttpContext Context = System.Web.HttpContext.Current;

        public OSBLEService()
            : base()
        {
            string userName = OsbleAuthentication.CurrentUser.UserName;

            currentUserProfile = db.UserProfiles.Where(u => u.UserName == userName).FirstOrDefault();

            if (Context.Session["ActiveCourse"] != null && (Context.Session["ActiveCourse"] is int))
            {
                int activeCourse = (int)Context.Cache["ActiveCourse"];

                currentCourse = (from c in db.AbstractCourses
                                 where c.ID == activeCourse
                                 select c).FirstOrDefault();

                currentCourseUser = (from c in db.CourseUsers
                                     where c.AbstractCourseID == currentCourse.ID
                                     && c.UserProfileID == currentUserProfile.ID
                                     select c).FirstOrDefault();
            }
        }
    }
}