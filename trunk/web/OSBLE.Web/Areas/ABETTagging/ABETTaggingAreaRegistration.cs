using System.Web.Mvc;

namespace OSBLE.Areas.ABETTagging
{
    public class ABETTaggingAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "ABETTagging";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "ABETTagging_default",
                "ABETTagging/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
