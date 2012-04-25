using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Caching;

namespace OSBLE.Utility
{
    public class FileCache : ObjectCache
    {
        private static string _cacheDir = HttpContext.Current.Server.MapPath("~\\App_Data\\Cache\\");
        private static int nameCounter = 1;
        private string _name = "";

        public FileCache()
        {
            _name = "FileCache_" + nameCounter;
            nameCounter++;
        }

        private string GetPath(string key, string regionName = null)
        {
            string directory = Path.Combine(_cacheDir, regionName);
            string filePath = Path.Combine(directory, key + ".dat");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return filePath;
        }

        public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            string path = GetPath(key, regionName);
            object oldData = null;

            //pull old value if it exists
            if (File.Exists(path))
            {
                oldData = Get(key, regionName);
            }
            using (FileStream stream = File.Open(path, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, value);
            }

            //As documented in the spec (http://msdn.microsoft.com/en-us/library/dd780602.aspx), return the old
            //cahced value or null
            return oldData;
        }

        public override CacheItem AddOrGetExisting(CacheItem value, CacheItemPolicy policy)
        {
            object oldData = AddOrGetExisting(value.Key, value.Value, policy);
            CacheItem returnItem = null;
            if (oldData != null)
            {
                returnItem = new CacheItem(value.Key)
                {
                    Value = oldData
                };
            }
            return returnItem;
        }

        public override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            return AddOrGetExisting(key, value, new CacheItemPolicy(), regionName);
        }

        public override bool Contains(string key, string regionName = null)
        {
            string path = GetPath(key, regionName);
            return File.Exists(path);
        }

        public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get 
            {
                //AC note: can use boolean OR "|" to set multiple flags.
                return System.Runtime.Caching.DefaultCacheCapabilities.CacheRegions;
            }
        }

        public override object Get(string key, string regionName = null)
        {
            string path = GetPath(key, regionName);
            object data = null;
            if (File.Exists(path))
            {
                FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);
                BinaryFormatter formatter = new BinaryFormatter();
                data = formatter.Deserialize(stream);
                stream.Close();
            }
            return data;
        }

        public override CacheItem GetCacheItem(string key, string regionName = null)
        {
            CacheItem item = (CacheItem)Get(key, regionName);
            return item;
        }

        public override long GetCount(string regionName = null)
        {
            string path = GetPath("", regionName);
            return Directory.GetFiles(path).Count();
        }

        protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override string Name
        {
            get { return _name; }
        }

        public override object Remove(string key, string regionName = null)
        {
            object valueToDelete = null;
            if(Contains(key))
            {
                valueToDelete = Get(key, regionName);
                string path = GetPath(key, regionName);
                File.Delete(path);
            }
            return valueToDelete;
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            Add(key, value, policy, regionName);
        }

        public override void Set(CacheItem item, CacheItemPolicy policy)
        {
            Add(item, policy);
        }

        public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            Add(key, value, absoluteExpiration, regionName);
        }

        public override object this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}