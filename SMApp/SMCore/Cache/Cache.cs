using System.Collections.Concurrent;

namespace SMApp
{
    /// <summary>
    /// 缓存操作接口实现
    /// </summary>
    internal class Cache : ICache
    {
        private static ConcurrentDictionary<string, object> _dic = null;
        public static ConcurrentDictionary<string, object> dic
        {
            get
            {
                if (_dic == null) _dic = new ConcurrentDictionary<string, object>();
                return _dic;
            }
        }

        public bool Containkey(string cacheKey)
        {
            return dic.ContainsKey(cacheKey);
        }

        public T GetCache<T>(string cacheKey) where T : class
        {
            return (T)dic[cacheKey];
        }
        public void RemoveCache(string cacheKey)
        {
            object obj;
            dic.TryRemove(cacheKey, out obj);
        }
        public void WriteCache<T>(T value, string cacheKey) where T : class
        {
            dic.TryAdd(cacheKey, value);
        }

    }
}
