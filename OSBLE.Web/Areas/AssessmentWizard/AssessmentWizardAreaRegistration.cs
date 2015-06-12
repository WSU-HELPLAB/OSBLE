using System.Web.Mvc;

namespace OSBLE.Areas.AssessmentWizard
{
    public class AssessmentWizardAreaRegistration : AreaRegistration
    {

        public static string AssessmentWizardRoute = "AssessmentWizard_default_sdfsdfsdf";
        public static string AssessmentWizardContentRoute = "AssessmentWizard_content_w3werwer";

        public override string AreaName
        {
            get
            {
                return "AssessmentWizard";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                AssessmentWizardAreaRegistration.AssessmentWizardContentRoute,
                "AssessmentWizard/Content/{*pathInfo}",
                new { action = "Index", controller = "Content" }
            );

            context.MapRoute(
                AssessmentWizardAreaRegistration.AssessmentWizardRoute,
                "AssessmentWizard/{controller}/{action}/{assessmentId}",
                new { action = "Index", controller = "Home", assessmentId = UrlParameter.Optional }
            );

        }
    }
}
