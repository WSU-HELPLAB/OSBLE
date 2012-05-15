using System.Web.Mvc;

namespace OSBLE.Areas.AssignmentDetails
{
    public class AssignmentDetailsAreaRegistration : AreaRegistration
    {
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
                "AssignmentDetails_default",
                "AssignmentDetails/{controller}/{action}/{assignmentId}",
                new { action = "Index", controller = "Details", assignmentId = UrlParameter.Optional }
            );
        }
    }
}
