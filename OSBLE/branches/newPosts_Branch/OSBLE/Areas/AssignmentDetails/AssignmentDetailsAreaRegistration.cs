using System.Web.Mvc;

namespace OSBLE.Areas.AssignmentDetails
{
    public class AssignmentDetailsAreaRegistration : AreaRegistration
    {
        public static string AssignmentDetailsRoute = "AssignmentDetails_default";
        public static string AssignmentDetailsShortcutRoute = "AssignmentDetails_shortcut";
        public static string AssignmentDetailsContentRoute = "AssignmentDetails_content";

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
                new { action = "Index", controller = "Home", assignmentId = UrlParameter.Optional }
            );

            context.MapRoute(
                AssignmentDetailsRoute,
                "AssignmentDetails/{controller}/{action}/{assignmentId}",
                new { action = "Index", controller = "Home", assignmentId = UrlParameter.Optional }
            );
        }
    }
}
