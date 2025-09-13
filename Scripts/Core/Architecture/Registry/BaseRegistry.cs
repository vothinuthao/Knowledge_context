using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenDeckbuilding.Core.Architecture.Registry
{
    /// <summary>
    /// Generic base registry implementation
    /// </summary>
    public abstract class BaseRegistry<TKey, TValue> : MonoBehaviour, IRegistry<TKey, TValue>
    {
        [Header("Registry Settings")]
        [SerializeField] protected bool enableLogging = true;
        [SerializeField] protected bool allowDuplicateKeys = false;
        
        protected Dictionary<TKey, TValue> _registry;
        protected List<TValue> _cachedValues;
        protected bool _cacheInvalid = true;
        
        public event Action<TKey, TValue> OnRegistered;
        public event Action<TKey, TValue> OnUnregistered;
        
        protected virtual void Awake()
        {
            _registry = new Dictionary<TKey, TValue>();
            _cachedValues = new List<TValue>();
        }
        
        public virtual void Register(TKey key, TValue value)
        {
            if (key == null)
            {
                Debug.LogError($"[{GetType().Name}] Cannot register with null key");
                return;
            }
            
            if (!allowDuplicateKeys && _registry.ContainsKey(key))
            {
                Debug.LogWarning($"[{GetType().Name}] Key already registered: {key}");
                return;
            }
            
            _registry[key] = value;
            _cacheInvalid = true;
            
            if (enableLogging)
                Debug.Log($"[{GetType().Name}] Registered: {key}");
            
            OnRegistered?.Invoke(key, value);
        }
        
        public virtual bool Unregister(TKey key)
        {
            if (_registry.TryGetValue(key, out TValue value))
            {
                _registry.Remove(key);
                _cacheInvalid = true;
                
                if (enableLogging)
                    Debug.Log($"[{GetType().Name}] Unregistered: {key}");
                
                OnUnregistered?.Invoke(key, value);
                return true;
            }
            
            return false;
        }
        
        public virtual TValue Get(TKey key)
        {
            _registry.TryGetValue(key, out TValue value);
            return value;
        }
        
        public virtual bool Contains(TKey key)
        {
            return _registry.ContainsKey(key);
        }
        
        public virtual IEnumerable<TValue> GetAll()
        {
            if (_cacheInvalid)
            {
                RefreshCache();
            }
            return _cachedValues;
        }
        
        public virtual IEnumerable<TKey> GetAllKeys()
        {
            return _registry.Keys;
        }
        
        public virtual void Clear()
        {
            _registry.Clear();
            _cachedValues.Clear();
            _cacheInvalid = false;
            
            if (enableLogging)
                Debug.Log($"[{GetType().Name}] Cleared all entries");
        }
        
        private void RefreshCache()
        {
            _cachedValues.Clear();
            _cachedValues.AddRange(_registry.Values);
            _cacheInvalid = false;
        }
        
        public int Count => _registry.Count;
    }
}