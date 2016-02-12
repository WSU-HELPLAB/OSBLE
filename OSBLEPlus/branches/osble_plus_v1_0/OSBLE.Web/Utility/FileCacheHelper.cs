using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;
using OSBLE.Models.Users;

namespace OSBLE.Utility
{
    ///TODO: remove this file from source control, it is not used anymore.
    //public class FileCacheHelper
    //{


    //    /*/// <summary>
    //    /// Returns a cache instance for global, cross-session information.  You probably
    //    /// should use <see cref="GetCacheInstance"/> instead.
    //    /// </summary>
    //    /// <returns></returns>
    //    public static FileCache GetGlobalCacheInstance()
    //    {
    //        FileCache fc = new FileCache(FileSystem.GetCachePath(), new ObjectBinder());
    //        fc.DefaultRegion = "global";
    //        fc.DefaultPolicy = new CacheItemPolicy() { SlidingExpiration = new TimeSpan(7, 0, 0, 0) };
    //        return fc;
    //    }*/
    //}
}