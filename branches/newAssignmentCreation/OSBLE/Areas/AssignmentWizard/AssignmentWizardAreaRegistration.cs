using System.Web.Mvc;

namespace OSBLE.Areas.AssignmentWizard
{
    public class AssignmentWizardAreaRegistration : AreaRegistration
    {
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
                "AssignmentWizard_default",
                "AssignmentWizard/{controller}/{action}/{assignmentId}",
                new { action = "Index", controller = "Home", assignmentId = UrlParameter.Optional }
            );
        }
    }
}
