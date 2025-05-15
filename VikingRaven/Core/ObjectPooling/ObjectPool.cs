using System;
using System.Collections.Generic;
using UnityEngine;

namespace VikingRaven.Core.ObjectPooling
{
    /// <summary>
    /// Generic Object Pool implementation for reusing game objects
    /// </summary>
    /// <typeparam name="T">Type of component to pool</typeparam>
    public class ObjectPool<T> where T : Component
    {
        // Original prefab to instantiate from
        private readonly T _prefab;
        
        // Parent transform for pooled objects
        private readonly Transform _parent;
        
        // List to store inactive (available) objects
        private readonly List<T> _inactiveObjects = new List<T>();
        
        // List to store all objects (active and inactive)
        private readonly List<T> _allObjects = new List<T>();
        
        // Action to execute when an object is returned to the pool
        private readonly Action<T> _onReturnToPool;
        
        // Action to execute when an object is taken from the pool
        private readonly Action<T> _onTakeFromPool;
        
        // Whether to create new objects when pool is empty
        private readonly bool _expandWhenEmpty;
        
        // Lock to ensure thread-safety
        private readonly object _lock = new object();
        
        /// <summary>
        /// Creates a new object pool
        /// </summary>
        /// <param name="prefab">The prefab to instantiate</param>
        /// <param name="initialCapacity">Initial number of instances to create</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        /// <param name="expandWhenEmpty">Whether to create new objects when pool is empty</param>
        /// <param name="onReturnToPool">Action to execute when object is returned to pool</param>
        /// <param name="onTakeFromPool">Action to execute when object is taken from pool</param>
        public ObjectPool(T prefab, int initialCapacity = 10, Transform parent = null, bool expandWhenEmpty = true,
            Action<T> onReturnToPool = null, Action<T> onTakeFromPool = null)
        {
            _prefab = prefab;
            _parent = parent;
            _expandWhenEmpty = expandWhenEmpty;
            _onReturnToPool = onReturnToPool;
            _onTakeFromPool = onTakeFromPool;
            
            // Pre-instantiate objects based on initial capacity
            PreWarm(initialCapacity);
        }
        
        /// <summary>
        /// Pre-instantiates a specified number of objects and adds them to the pool
        /// </summary>
        /// <param name="count">Number of objects to instantiate</param>
        public void PreWarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateNewObject();
            }
        }
        
        /// <summary>
        /// Takes an object from the pool, creating a new one if necessary
        /// </summary>
        /// <returns>An instance of type T</returns>
        public T Get()
        {
            lock (_lock)
            {
                T obj = null;
                
                // Try to get an inactive object
                if (_inactiveObjects.Count > 0)
                {
                    obj = _inactiveObjects[_inactiveObjects.Count - 1];
                    _inactiveObjects.RemoveAt(_inactiveObjects.Count - 1);
                }
                else if (_expandWhenEmpty) // Create new object if allowed
                {
                    obj = CreateNewObject();
                    _inactiveObjects.Remove(obj); // Remove from inactive list
                }
                else
                {
                    Debug.LogWarning($"Object pool for {typeof(T).Name} is empty and not allowed to expand.");
                    return null;
                }
                
                // Prepare object for use
                obj.gameObject.SetActive(true);
                
                // Execute callback
                _onTakeFromPool?.Invoke(obj);
                
                return obj;
            }
        }
        
        /// <summary>
        /// Returns an object to the pool
        /// </summary>
        /// <param name="obj">The object to return</param>
        public void Return(T obj)
        {
            if (obj == null) return;
            
            lock (_lock)
            {
                // Skip if already in the inactive list
                if (_inactiveObjects.Contains(obj))
                {
                    Debug.LogWarning($"Object {obj.name} is already in the pool.");
                    return;
                }
                
                // Execute callback
                _onReturnToPool?.Invoke(obj);
                
                // Deactivate the object and add to inactive list
                obj.gameObject.SetActive(false);
                _inactiveObjects.Add(obj);
            }
        }
        
        /// <summary>
        /// Clears all objects from the pool
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                foreach (var obj in _allObjects)
                {
                    if (obj != null)
                    {
                        GameObject.Destroy(obj.gameObject);
                    }
                }
                
                _inactiveObjects.Clear();
                _allObjects.Clear();
            }
        }
        
        /// <summary>
        /// Gets the number of inactive objects in the pool
        /// </summary>
        public int CountInactive => _inactiveObjects.Count;
        
        /// <summary>
        /// Gets the total number of objects managed by this pool
        /// </summary>
        public int CountAll => _allObjects.Count;
        
        /// <summary>
        /// Creates a new object and adds it to the pool
        /// </summary>
        /// <returns>The created object</returns>
        private T CreateNewObject()
        {
            var obj = GameObject.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            obj.name = $"{_prefab.name}_{_allObjects.Count}";
            
            _inactiveObjects.Add(obj);
            _allObjects.Add(obj);
            
            return obj;
        }
    }
}