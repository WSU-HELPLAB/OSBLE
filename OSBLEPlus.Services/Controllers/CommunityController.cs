using System.Web.Http;
using OSBLEPlus.Logic.Utility;
using System.Configuration;

namespace OSBLEPlus.Services.Controllers
{
    public class CommunityController : ApiController
    {
        /// <summary>
        /// Returns the Assembly's Library Version Number as a string
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string CommunityEnabled()
        {
            return ConfigurationManager.AppSettings["CommunityEnabled"];
        }
    }
}