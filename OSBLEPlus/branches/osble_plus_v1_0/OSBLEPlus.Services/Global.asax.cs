using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace OSBLEPlus.Services
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            AreaRegistration.RegisterAllAreas();
        }
    }
}
