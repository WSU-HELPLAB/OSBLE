using System;
using System.IO;
using System.Runtime.Caching;
using System.Web;

namespace OSBLEPlus.Logic.Utility
{
    public class FileCacheHelper
    {
        public static string GetCachePath()
        {
            return GetCachePath(HttpContext.Current.Server.MapPath("~\\App_Data\\"));
        }
        public static string GetCachePath(string path)
        {
            return Path.Combine(path, "Cache");
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
        /// Returns a cache instance for global, cross-session information.  You probably
        /// should use <see cref="GetCacheInstance"/> instead.
        /// </summary>
        /// <returns></returns>
        public static FileCache GetGlobalCacheInstance(string cacheRoot)
        {
            var fc = new FileCache(cacheRoot, new ObjectBinder())
            {
                DefaultRegion = "global",
                DefaultPolicy = new CacheItemPolicy {SlidingExpiration = new TimeSpan(7, 0, 0, 0)}
            };
            return fc;
        }
    }
}