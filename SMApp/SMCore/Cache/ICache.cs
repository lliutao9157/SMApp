namespace SMApp
{
    /// <summary>
    /// 缓存操作接口
    /// </summary>
    public interface ICache
    {
        T GetCache<T>(string cacheKey) where T : class;
        void WriteCache<T>(T value, string cacheKey) where T : class;
        void RemoveCache(string cacheKey);
        bool Containkey(string cacheKey);

    }
}
