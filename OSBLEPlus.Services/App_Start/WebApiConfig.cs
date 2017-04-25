using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Http.ModelBinding.Binders;
using Newtonsoft.Json.Serialization;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Services.Attributes;
using OSBLEPlus.Services.Models;

namespace OSBLEPlus.Services
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var jsonFormatter = config.Formatters.OfType<JsonMediaTypeFormatter>().First();
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver
            {
                IgnoreSerializableAttribute = true
            };

            // Web API configuration and services
            config.EnableCors(new AllowWebClientsAttribute());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { action="get", id = RouteParameter.Optional }
            );

            var provider = new SimpleModelBinderProvider(typeof(SubmissionRequest), new SubmitEventModelBinder());
            config.Services.Insert(typeof(ModelBinderProvider), 0, provider);
        }
    }
}
