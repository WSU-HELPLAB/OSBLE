using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace OSBLE.Web
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
                new[] { "OSBLE.Controllers" }
                );

            //very generic.  Make sure that these stay at the bottom.
            routes.MapRoute(
                "File System",
                "FileSystem/{*pathInfo}",
                new { controller = "Home", action = "NoAccess" }
            );

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }, // Parameter defaults
                new[] { "OSBLE.Controllers" }
            );
        }
    }
}