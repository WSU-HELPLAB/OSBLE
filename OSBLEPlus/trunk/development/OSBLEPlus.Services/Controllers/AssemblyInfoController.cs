using System.Web.Http;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Services.Controllers
{
    public class AssemblyInfoController : ApiController
    {
        public string VersionNumber()
        {
            return StringConstants.LibraryVersion;
        }
    }
}