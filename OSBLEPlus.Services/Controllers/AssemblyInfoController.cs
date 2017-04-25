using System.Web.Http;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Services.Controllers
{
    public class AssemblyInfoController : ApiController
    {
        /// <summary>
        /// Returns the Assembly's Library Version Number as a string
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string VersionNumber()
        {
            return StringConstants.LibraryVersion;
        }
    }
}