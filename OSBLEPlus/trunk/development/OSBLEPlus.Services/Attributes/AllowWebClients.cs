using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http.Cors;

namespace OSBLEPlus.Services.Attributes
{
    public class AllowWebClientsAttribute : Attribute, ICorsPolicyProvider
    {
        private readonly string _corsConfigKey = "cors:allowOrigins";
        private CorsPolicy _policy;

        public AllowWebClientsAttribute(string configKey = null)
        {
            if (!string.IsNullOrWhiteSpace(configKey))
                _corsConfigKey = configKey;
        }

        public Task<CorsPolicy> GetCorsPolicyAsync(
                                    HttpRequestMessage request,
                                    CancellationToken cancellationToken)
        {
            if (_policy != null) return Task.FromResult(_policy);

            var corsPolicy = new CorsPolicy
            {
                AllowAnyHeader = true,
                AllowAnyMethod = true,
                AllowAnyOrigin = false
            };

            var appSettings = ConfigurationManager.AppSettings[_corsConfigKey];

            if (!string.IsNullOrEmpty(appSettings))
            {
                foreach (var setting in from s in appSettings.Split(';')
                                           where !string.IsNullOrEmpty(s)
                                           select s)
                {
                    corsPolicy.Origins.Add(setting);
                }
            }

            _policy = corsPolicy;

            return Task.FromResult(_policy);
        }
    }
}