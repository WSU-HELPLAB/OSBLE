using System.Configuration;

namespace OSBLEPlus.Logic.DomainObjects
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
    }
}
