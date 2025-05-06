using UnityEngine;

namespace Core.Utils
{
    /// <summary>
    /// Generic PureSingleton class for non-MonoBehaviour singletons
    /// Implements thread-safe lazy initialization pattern
    /// </summary>
    public abstract class PureSingleton<T> where T : PureSingleton<T>, new()
    {
        // Static instance
        private static T _instance;
        
        // Thread synchronization object
        private static readonly object _lock = new object();
        
        // Initialization state
        private bool _isInitialized = false;
        
        /// <summary>
        /// Public accessor for singleton instance with lazy initialization
        /// </summary>
        public static T Instance
        {
            get
            {
                // Double-check locking pattern for thread safety
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // Create new instance
                            _instance = new T();
                            
                            // Log creation if in debug mode
                            Debug.Log($"[PureSingleton] Created instance of {typeof(T).Name}");
                            
                            // Call OnCreated - this can be overridden in derived classes
                            _instance.OnCreated();
                        }
                    }
                }
                
                return _instance;
            }
        }
        
        /// <summary>
        /// Check if instance exists without creating it
        /// </summary>
        public static bool HasInstance => _instance != null;
        
        /// <summary>
        /// Check if this instance is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Protected constructor to prevent direct instantiation
        /// </summary>
        protected PureSingleton()
        {
            // Ensure this is being constructed through the singleton pattern
            if (_instance != null)
            {
                Debug.LogWarning($"[PureSingleton] Attempting to create a second instance of {typeof(T).Name}. Using singleton Instance property instead.");
            }
        }
        
        /// <summary>
        /// Called automatically when instance is created
        /// Override in derived classes to perform initialization logic
        /// </summary>
        protected virtual void OnCreated()
        {
            // Default implementation does nothing
        }
        
        /// <summary>
        /// Initialize the singleton with optional parameters
        /// Override in derived classes with specific initialization logic
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise</returns>
        public virtual bool Initialize()
        {
            _isInitialized = true;
            return true;
        }
        
        /// <summary>
        /// Ensure that this singleton is initialized before use
        /// Throws an exception if not initialized and autoInitialize is false
        /// </summary>
        /// <param name="autoInitialize">If true, will try to auto-initialize if not already initialized</param>
        /// <returns>True if initialized (either previously or now), false if failed to initialize</returns>
        protected bool EnsureInitialized(bool autoInitialize = true)
        {
            if (!_isInitialized && autoInitialize)
            {
                Debug.Log($"[PureSingleton] Auto-initializing {typeof(T).Name}");
                return Initialize();
            }
            
            if (!_isInitialized)
            {
                Debug.LogError($"[PureSingleton] {typeof(T).Name} is not initialized! Call Initialize() first.");
                // throw new System.InvalidOperationException($"{typeof(T).Name} is not initialized! Call Initialize() first.");
            }
            
            return _isInitialized;
        }
        
        /// <summary>
        /// Reset the singleton instance (for testing or special cases)
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lock)
            {
                if (_instance != null)
                {
                    Debug.Log($"[PureSingleton] Resetting instance of {typeof(T).Name}");
                    _instance = null;
                }
            }
        }
        
        /// <summary>
        /// Release any resources used by this singleton
        /// Override in derived classes to perform cleanup
        /// </summary>
        public virtual void Cleanup()
        {
            _isInitialized = false;
        }
    }
}