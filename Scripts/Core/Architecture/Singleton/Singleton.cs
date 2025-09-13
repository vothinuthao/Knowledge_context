namespace RavenDeckbuilding.Core.Architecture.Singleton
{
    public abstract class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static readonly object _lock = new object();

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    _instance ??= new T();
                    return _instance;
                }
            }
        }

        protected Singleton()
        {
            if (_instance != null)
            {
                throw new System.InvalidOperationException("Singleton instance already exists!");
            }
        }

        public static void DestroyInstance()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }
    }
}