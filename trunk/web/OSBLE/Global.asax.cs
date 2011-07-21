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
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.RouteExistingFiles = true;
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
                "File System",
                "FileSystem/{*pathInfo}",
                new { controller = "Home", action = "NoAccess" }
            );

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