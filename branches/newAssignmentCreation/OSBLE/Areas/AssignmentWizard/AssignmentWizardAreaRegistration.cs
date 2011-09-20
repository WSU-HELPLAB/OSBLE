using System.Web.Mvc;

namespace OSBLE.Areas.AssignmentWizard
{
    public class AssignmentWizardAreaRegistration : AreaRegistration
    {
        public static string AssignmentWizardRoute = "AssignmentWizard_default";
        public static string AssignmentWizardContentRoute = "AssignmentWizard_content";

        public override string AreaName
        {
            get
            {
                return "AssignmentWizard";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                AssignmentWizardAreaRegistration.AssignmentWizardContentRoute,
                "AssignmentWizard/Content/{*pathInfo}",
                new { action = "Index", controller = "Content" }
            );

            context.MapRoute(
                AssignmentWizardAreaRegistration.AssignmentWizardRoute,
                "AssignmentWizard/{controller}/{action}/{assignmentId}",
                new { action = "Index", controller = "Home", assignmentId = UrlParameter.Optional }
            );
        }
    }
}
