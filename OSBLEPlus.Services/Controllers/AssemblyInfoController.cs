using System.Web.Http;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Services.Controllers
{
    public class AssemblyInfoController : ApiController
    {
        [HttpGet]
        public string VersionNumber()
        {
            return StringConstants.LibraryVersion;
        }
    }
}