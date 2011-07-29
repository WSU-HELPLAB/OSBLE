using System.Web.Mvc;
using System.Web.Routing;

namespace OSBLE
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode,
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            //***When defining routes, be sure to go from specific to generic routes***

            //because the file system is exposed to the web, we need to prevent direct
            //file access
            routes.RouteExistingFiles = false;

            //allow direct access to web services.
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{resource}.svc/{*pathInfo}");
            routes.IgnoreRoute("{filename}.svc/{*pathInfo}");


            routes.IgnoreRoute("Content/{*pathInfo}");
            routes.IgnoreRoute("Scripts/{*pathInfo}");
            routes.IgnoreRoute("ClientBin/{*pathInfo}");
            routes.IgnoreRoute("Services/{*pathInfo}");
            routes.IgnoreRoute("clientaccesspolicy.xml");

            //for debugging in-browser silverlight apps
#if DEBUG

            routes.IgnoreRoute("crossdomain.xml");
            routes.IgnoreRoute("OsbleRubricTestPage.html");
            routes.IgnoreRoute("OsbleRubricTestPage.aspx");
            routes.IgnoreRoute("SilverlightSandboxTestPage.html");
            routes.IgnoreRoute("SilverlightSandboxTestPage.aspx");
            routes.IgnoreRoute("ViewPeerReviewTestPage.aspx");
            routes.IgnoreRoute("ViewPeerReviewTestPage.html");
            routes.IgnoreRoute("EditPeerReviewTestPage.aspx");
            routes.IgnoreRoute("EditPeerReviewTestPage.html");
#endif

            routes.MapRoute(
                "FileHandler-Course",
                "FileHandler/CourseDocument/{courseId}/{filePath}",
                new { controller = "FileHandler", action = "CourseDocument" }
                );

            routes.MapRoute(
                "Rubric-eval",
                "Rubric/{AbstractAssignmentActivityId}/{teamUserId}",
                new { controller = "Rubric", action = "Index" }
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
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );
        }

        protected void Application_Start()
        {
#if DEBUG
            //Development only.
            System.Data.Entity.Database.SetInitializer(new OSBLE.Models.OSBLEContextModelChangeInitializer());
            //System.Data.Entity.Database.SetInitializer(new OSBLE.Models.OSBLEContextAlwaysCreateInitializer());
#endif

            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }


    }
}