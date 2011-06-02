
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


    // TODO: Create methods containing your application logic.
    [EnableClientAccess()]
    [RequiresAuthentication]
    public class DomainService1 : DomainService
    {
        private OSBLEContext context = new OSBLEContext();

        public IEnumerable<Course> GetCourses()
        {
            HttpContext httpContext = (HttpContext)ServiceContext.GetService(typeof(HttpContext));

            int activeCourse = (int)httpContext.Session["ActiveCourse"];

            return context.Courses.Where(c => c.ID == activeCourse);
        }
    }
}


