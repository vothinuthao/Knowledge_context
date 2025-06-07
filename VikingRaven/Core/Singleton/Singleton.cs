// Singleton.cs

using System;
using UnityEngine;

namespace Core.Utils
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        
        [Obsolete("Obsolete")]
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                    _instance = FindObjectOfType<T>();
                    if (!_instance)
                    {
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        _instance = singletonObject.AddComponent<T>();
                    }
                }
                
                return _instance;
            }
        }
        public static bool HasInstance => _instance != null;
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"More than one instance of {typeof(T).Name} found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            
            _instance = this as T;
            
            DontDestroyOnLoad(gameObject);
            OnInitialize();
        }
        protected virtual void OnInitialize()
        {
        }
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}