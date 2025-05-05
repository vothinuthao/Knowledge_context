// Singleton.cs

using UnityEngine;

namespace Core.Utils
{
    /// <summary>
    /// Generic Singleton base class for managers
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // Static instance for the singleton
        private static T _instance;
        
        // Public accessor with lazy instantiation
        public static T Instance
        {
            get
            {
                // If the instance doesn't exist, try to find it in the scene
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    
                    // If instance is still null, create a new GameObject with the component
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        _instance = singletonObject.AddComponent<T>();
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
        /// Called when the instance is created
        /// </summary>
        protected virtual void Awake()
        {
            // If an instance already exists and it's not this one
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"More than one instance of {typeof(T).Name} found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            
            // Set the instance
            _instance = this as T;
            
            // Optionally, make the object persist between scenes
            DontDestroyOnLoad(gameObject);
            
            // Initialize the singleton
            OnInitialize();
        }
        
        /// <summary>
        /// Called when the singleton is initialized
        /// Override this in derived classes
        /// </summary>
        protected virtual void OnInitialize()
        {
            // Override in derived classes
        }
        
        /// <summary>
        /// Called when the MonoBehaviour will be destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            // If this is the current instance
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}