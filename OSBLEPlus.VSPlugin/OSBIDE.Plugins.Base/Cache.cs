using System.Runtime.Caching;
using OSBLEPlus.Logic.Utility;

namespace OSBIDE.Plugins.Base
{
    public static class Cache
    {
        public static FileCache CacheInstance
        {
            get
            {
                return new FileCache(StringConstants.LocalCacheDirectory);
            }
        }
    }
}
