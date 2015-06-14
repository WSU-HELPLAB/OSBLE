using System.Configuration;

namespace OSBLEPlus.Logic.Utility
{
    public static class StringConstants
    {
        public static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["OSBLEData"].ConnectionString;
            }
        }

        public static string DataServiceRoot
        {
            get { return ConfigurationManager.AppSettings["OSBLEDataService"]; }
        }

        public const string SqlHelperScopeIdentityName = "@scope_id";
    }
}
