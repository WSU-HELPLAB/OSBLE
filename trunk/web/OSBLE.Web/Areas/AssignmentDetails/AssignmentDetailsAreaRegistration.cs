using System.Web.Mvc;

namespace OSBLE.Areas.AssignmentDetails
{
    public class AssignmentDetailsAreaRegistration : AreaRegistration
    {
        public static string AssignmentDetailsRoute = "AssignmentDetails_default_qwerwerwer";
        public static string AssignmentDetailsShortcutRoute = "AssignmentDetails_shortcut_sdfsdfsdf";
        public static string AssignmentDetailsContentRoute = "AssignmentDetails_content_sdfsdfdsf";

        public override string AreaName
        {
            get
            {
                return "AssignmentDetails";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                AssignmentDetailsContentRoute,
                "AssignmentDetails/Content/{*pathInfo}",
                new { action = "Index", controller = "Content" }
            );

            context.MapRoute(
                AssignmentDetailsShortcutRoute,
                "AssignmentDetails/{assignmentId}",
                new { action = "Index", controller = "Home", assignmentId = UrlParameter.Optional },
                namespaces: new[] { "OSBLE.Areas.AssignmentDetails.Controllers" }
            );

            context.MapRoute(
                AssignmentDetailsRoute,
                "AssignmentDetails/{controller}/{action}/{assignmentId}",
                new { action = "Index", controller = "Home", assignmentId = UrlParameter.Optional },
                namespaces: new[] { "OSBLE.Areas.AssignmentDetails.Controllers" }
            );
        }
    }
}
