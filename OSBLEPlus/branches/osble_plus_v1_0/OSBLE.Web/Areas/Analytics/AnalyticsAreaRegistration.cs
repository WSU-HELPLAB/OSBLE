using System.Web.Mvc;

namespace OSBLE.Areas.Analytics
{
    public class AnalyticsAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Analytics";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Analytics_default",
                "Analytics/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
