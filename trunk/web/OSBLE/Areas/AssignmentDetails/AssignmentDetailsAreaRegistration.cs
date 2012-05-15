using System.Web.Mvc;

namespace OSBLE.Areas.AssignmentDetails
{
    public class AssignmentDetailsAreaRegistration : AreaRegistration
    {
        public static string AssignmentDetailsRoute = "AssignmentDetails_default";
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
                AssignmentDetailsRoute,
                "AssignmentDetails/{assignmentId}",
                new { action = "Index", controller = "Home", assignmentId = UrlParameter.Optional }
            );
        }
    }
}
