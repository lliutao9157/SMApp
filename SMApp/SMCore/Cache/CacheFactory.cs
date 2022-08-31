namespace SMApp
{
    /// <summary>
    /// 缓存操作对外入口
    /// </summary>
     class CacheFactory
    {
        private static ICache _Cashe = null;
        internal static ICache Cache
        {
            get
            {
                if (_Cashe == null)
                    _Cashe = new Cache();
                return _Cashe;
            }
        }
    }
}
