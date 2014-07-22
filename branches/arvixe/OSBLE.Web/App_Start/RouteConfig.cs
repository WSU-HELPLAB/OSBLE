using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace OSBLE
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            //allow direct access to web services.
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{resource}.svc/{*pathInfo}");
            routes.IgnoreRoute("{filename}.svc/{*pathInfo}");


            routes.IgnoreRoute("Content/{*pathInfo}");
            routes.IgnoreRoute("Scripts/{*pathInfo}");
            routes.IgnoreRoute("ClientBin/{*pathInfo}");
            routes.IgnoreRoute("Services/{*pathInfo}");
            routes.IgnoreRoute("clientaccesspolicy.xml");            

            //custom path to iCalendar subscription methods
            routes.MapRoute(
                "iCalendar Subscribe",
                "iCal/{courseId}",
                new { Controller = "iCalendar", action = "Test" }, //TODO: change to proper method
                new { courseId = @"\d+" });

            routes.MapRoute(
                "User identities",
                "user/{id}/{action}",
                new { controller = "User", action = "Identity", id = string.Empty, anon = false });
            routes.MapRoute(
                "PPID identifiers",
                "anon",
                new { controller = "User", action = "Identity", id = string.Empty, anon = true });

            routes.MapRoute(
                "FileHandler-Course",
                "FileHandler/CourseDocument/{courseId}/{filePath}",
                new { controller = "FileHandler", action = "CourseDocument" }
                );

            routes.MapRoute(
                "Rubric-eval",
                "Rubric/{AbstractAssignmentActivityId}/{teamUserId}",
                new { controller = "Rubric", action = "Index" },
                namespaces: new[] { "OSBLE.Controllers" }
                );

            //very generic.  Make sure that these stay at the bottom.
            routes.MapRoute(
                "File System",
                "FileSystem/{*pathInfo}",
                new { controller = "Home", action = "NoAccess" },
                namespaces: new[] { "OSBLE.Controllers" }
            );

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }, // Parameter defaults
                namespaces: new[] { "OSBLE.Controllers" }
            );
        }
    }
}