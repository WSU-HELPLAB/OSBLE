using System.Reflection;

namespace WashingtonStateUniversity.OSBIDE_Plugins_VS2013
{
    public sealed partial class OsbidePluginsVs2013Package
    {
        private static void LoadAssemblies()
        {
            Assembly.Load("OSBLEPlus.Library.Shell.Signed");
            Assembly.Load("OSBLEPlus.Logic.Shell.Signed");
            Assembly.Load("OSBIDE.Library.ServiceClient");
            Assembly.Load("OSBIDE.Controls");
            Assembly.Load("OSBIDE.Plugins.Base");
        }

        public void Dispose()
        {
            _encoder.Dispose();
        }
    }
}
