using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;
using OSBLE.Models.Users;

namespace OSBLE.Utility
{
    public class FileCacheHelper
    {
        /// <summary>
        /// Returns a <see cref="FileCache"/> with the default region tailored to the
        /// specified user
        /// </summary>
        /// <param name="client">The user who is accessing the file cache (typically the
        /// person making the web request)</param>
        /// <returns></returns>
        public static FileCache GetCacheInstance(UserProfile client)
        {
            FileCache fc = new FileCache(FileSystem.GetCachePath(), new ObjectBinder());
            fc.DefaultRegion = client.ID.ToString();
            fc.DefaultPolicy = new CacheItemPolicy() { SlidingExpiration = new TimeSpan(7, 0, 0, 0) };
            return fc;
        }

        /// <summary>
        /// Returns a cache instance for global, cross-session information.  You probably
        /// should use <see cref="GetCacheInstance"/> instead.
        /// </summary>
        /// <returns></returns>
        public static FileCache GetGlobalCacheInstance()
        {
            FileCache fc = new FileCache(FileSystem.GetCachePath(), new ObjectBinder());
            fc.DefaultRegion = "global";
            fc.DefaultPolicy = new CacheItemPolicy() { SlidingExpiration = new TimeSpan(7, 0, 0, 0) };
            return fc;
        }
    }
}