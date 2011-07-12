
namespace OSBLE.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
    using OSBLE.Models;
    using OSBLE.Models.Courses;
    using System.Runtime.Serialization;
    using System.Web.Configuration;
    using System.Web;
    using OSBLE.Models.Users;


    /// <summary>
    /// Provides current user and active course context,
    /// as well as database access
    /// for services that inherit from it.
    /// </summary>
    [EnableClientAccess()]
    [RequiresAuthentication]
    public class OSBLEService : DomainService
    {
        protected OSBLEContext db = new OSBLEContext();
        protected CoursesUsers CurrentCourseUser = null;
        protected UserProfile currentUserProfile = null;

        protected HttpContext Context = System.Web.HttpContext.Current;
                
        public OSBLEService()
            : base()
        {
            string userName = Context.User.Identity.Name;

            currentUserProfile = db.UserProfiles.Where(u => u.UserName == userName).FirstOrDefault();

            if (Context.Session["ActiveCourse"] != null && (Context.Session["ActiveCourse"] is int))
            {
                int activeCourse = (int)Context.Session["ActiveCourse"];


                CurrentCourseUser = db.CoursesUsers.Where(cu => cu.AbstractCourseID == activeCourse &&
                                                    cu.UserProfileID == currentUserProfile.ID &&
                                                    cu.AbstractCourse is Course &&
                                                    (!(cu.AbstractCourse as Course).Inactive ||
                                                        cu.AbstractRoleID == (int)Privileges.CourseRoles.Instructor ||
                                                        cu.AbstractRoleID == (int)Privileges.CourseRoles.Observer
                                                    )).FirstOrDefault();
            }
        }
    }
}


