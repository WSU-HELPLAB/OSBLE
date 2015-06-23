using System;
using System.Configuration;
using System.IO;
using System.Reflection;

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

        public static string WebClientRoot
        {
            get { return ConfigurationManager.AppSettings["OSBLEWeb"]; }
        }
        public static string DataServiceRoot
        {
            get { return ConfigurationManager.AppSettings["OSBLEDataService"]; }
        }

        public static string ActivityFeedUrl
        {
            get { return string.Format("{0}/Feed", WebClientRoot); }
        }

        public static string AskTheProfessorUrl
        {
            get { return string.Format("{0}/PrivateQuestion", WebClientRoot); }
        }

        public static string CreateAccountUrl
        {
            get { return string.Format("{0}/Account/Create", WebClientRoot); }
        }

        public static string ChatUrl
        {
            get { return string.Format("{0}/Chat", WebClientRoot); }
        }

        public static string ProfileUrl
        {
            get { return string.Format("{0}/Profile", WebClientRoot); }
        }

        public static string UpdateUrl
        {
            get { return string.Format("{0}/Content/osble.zip", WebClientRoot); }
        }

        public static string LocalUpdatePath
        {
            get { return Path.Combine(DataRoot, "osbide_debug.vsix"); }
        }

        public static string DataRoot
        {
            get
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OSBLE");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public static string LocalCacheDirectory
        {
            get
            {
                var path = Path.Combine(DataRoot, ConfigurationManager.AppSettings["LocalCacheDirectory"]);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public static string LocalErrorLogExtension
        {
            get
            {
                return ".log";
            }
        }

        public static string LocalErrorLogFileName
        {
            get
            {
                return DateTime.Today.ToString("yyyy-MM-dd");
            }
        }

        public static string LocalErrorLogPath
        {
            get
            {
                return Path.Combine(DataRoot, string.Format("{0}.{1}", LocalErrorLogFileName, LocalErrorLogExtension));
            }
        }

        public static string AesKeyCacheKey
        {
            get
            {
                return "AesKey";
            }
        }

        public static string AesVectorCacheKey
        {
            get
            {
                return "AesVector";
            }
        }

        public static string UserNameCacheKey
        {
            get
            {
                return "UserName";
            }
        }

        public static string PasswordCacheKey
        {
            get
            {
                return "Password";
            }
        }

        public static string AuthenticationCacheKey
        {
            get
            {
                return "AuthKey";
            }
        }

        public static string LibraryVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public const string SqlHelperScopeIdentityName = "@scope_id";
    }
}
