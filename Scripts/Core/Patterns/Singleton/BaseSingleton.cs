// Assets/Scripts/Core/Patterns/Singleton.cs

using UnityEngine;

namespace Core.Patterns
{
    /// <summary>
    /// Pure C# Singleton pattern implementation (non-MonoBehaviour)
    /// </summary>
    /// <typeparam name="T">Type of the singleton class</typeparam>
    public class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static readonly object LockObject = new object();
        
        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (LockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                        }
                    }
                }
                
                return _instance;
            }
        }
        
        /// <summary>
        /// Protected constructor to prevent direct instantiation
        /// </summary>
        protected Singleton() 
        {
            if (_instance != null)
            {
                Debug.LogError($"[Singleton] Trying to create a second instance of {typeof(T).Name}");
            }
        }
        
        /// <summary>
        /// Reset the singleton instance (useful for testing)
        /// </summary>
        public static void ResetInstance()
        {
            lock (LockObject)
            {
                _instance = null;
            }
        }
    }
    
    /// <summary>
    /// MonoBehaviour-based Singleton pattern implementation
    /// </summary>
    /// <typeparam name="T">Type of the singleton MonoBehaviour</typeparam>
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isApplicationQuitting = false;
        
        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isApplicationQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T).Name}' already destroyed on application quit. Won't create again.");
                    return null;
                }
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                        _instance = singletonObject.AddComponent<T>();
                        (_instance as MonoBehaviourSingleton<T>)?.OnSingletonCreated();
                    }
                }
                
                return _instance;
            }
        }
        
        /// <summary>
        /// Virtual method called when the singleton is first created
        /// Override to customize initialization
        /// </summary>
        protected virtual void OnSingletonCreated() { }
        
        /// <summary>
        /// Virtual method called when the singleton is being destroyed
        /// Override to customize cleanup
        /// </summary>
        protected virtual void OnSingletonDestroyed() { }
        
        /// <summary>
        /// Called when the component awakens
        /// </summary>
        protected virtual void Awake()
        {
            CheckSingletonInstance();
        }
        
        /// <summary>
        /// Check if this is the singleton instance and set it up
        /// </summary>
        private void CheckSingletonInstance()
        {
            // If instance already exists and it's not us, destroy ourselves
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] Another instance of {typeof(T).Name} already exists. Destroying this duplicate.");
                Destroy(gameObject);
                return;
            }
            
            // If we're the first instance, make ourselves the singleton
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
        }
        
        /// <summary>
        /// Called when the application is quitting
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
            OnSingletonDestroyed();
        }
        
        /// <summary>
        /// Called when the component is being destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                OnSingletonDestroyed();
                _instance = null;
            }
        }
    }
}