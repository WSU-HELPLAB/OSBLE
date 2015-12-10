using System;
using System.Configuration;
using System.IO;
using System.Runtime.Caching;
using System.Web;
using System.Web.Hosting;
using OSBLE;
using OSBLE.Models.Users;

namespace OSBLEPlus.Logic.Utility
{
    public class FileCacheHelper
    {
        public static string GetCachePath()
        {
            return ConfigurationManager.AppSettings["FileCacheDirectory"];
        }        

        /// <summary>
        /// Returns a <see cref="FileCache"/> with the default region tailored to the
        /// specified user
        /// </summary>
        /// <returns></returns>
        public static FileCache GetCacheInstance(int userId)
        {
            var fc = new FileCache(GetCachePath(), new ObjectBinder())
            {
                DefaultRegion = userId.ToString(),
                DefaultPolicy = new CacheItemPolicy {SlidingExpiration = new TimeSpan(7, 0, 0, 0)}
            };
            return fc;
        }

        /// <summary>
        /// Returns a <see cref="FileCache"/> with the default region tailored to the
        /// specified user
        /// </summary>
        /// <param name="client">The user who is accessing the file cache (typically the
        /// person making the web request)</param>
        /// <returns></returns>
        public static FileCache GetCacheInstance(UserProfile client)
        {
            FileCache fc = new FileCache(GetCachePath(), new ObjectBinder())
            {
                DefaultRegion = client.ID.ToString(),
                DefaultPolicy = new CacheItemPolicy() {SlidingExpiration = new TimeSpan(7, 0, 0, 0)},
                MaxCacheSize = 2000000000 // 2GB
            };          
            
            return fc;
        }

        /// <summary>
        /// Returns a cache instance for global, cross-session information.  You probably
        /// should use <see cref="GetCacheInstance"/> instead.
        /// </summary>
        /// <returns></returns>
        public static FileCache GetGlobalCacheInstance()
        {
            var fc = new FileCache(GetCachePath(), new ObjectBinder())
            {
                DefaultRegion = "global",
                DefaultPolicy = new CacheItemPolicy {SlidingExpiration = new TimeSpan(7, 0, 0, 0)}
            };
            return fc;
        }
    }
}