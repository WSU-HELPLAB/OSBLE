/// This class is for using SignalR to auto-update
/// the activity feed when someone else adds a new 
/// item or reply.

using Owin;
using Microsoft.Owin;
[assembly: OwinStartup(typeof(OSBLE.Startup))]

namespace OSBLE
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Any connection or hub wire up and configuration should go here
            app.MapSignalR();
        }
    }
}