using System.ServiceModel.DomainServices.Hosting;
using System.ServiceModel.DomainServices.Server.ApplicationServices;

namespace OSBLE.Services
{
    [EnableClientAccess]
    public class AuthenticationDomainService1 : AuthenticationBase<User>
    {
        // To enable Forms/Windows Authentication for the Web Application, edit the appropriate section of web.config file.
    }

    public class User : UserBase
    {
        // NOTE: Profile properties can be added here
        // To enable profiles, edit the appropriate section of web.config file.

        // public string MyProfileProperty { get; set; }
    }
}