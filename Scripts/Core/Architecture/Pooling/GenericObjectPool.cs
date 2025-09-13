using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenDeckbuilding.Core.Architecture.Pooling
{
    /// <summary>
    /// Generic object pool implementation
    /// </summary>
    public class GenericObjectPool<T> : IObjectPool<T> where T : class, IPoolable
    {
        private readonly Queue<T> _pool;
        private readonly HashSet<T> _activeObjects;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _destroyFunc;
        private readonly int _maxSize;
        
        public int CountActive => _activeObjects.Count;
        public int CountInactive => _pool.Count;
        
        public GenericObjectPool(Func<T> createFunc, Action<T> destroyFunc = null, int initialSize = 10, int maxSize = 100)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _destroyFunc = destroyFunc;
            _maxSize = maxSize;
            
            _pool = new Queue<T>(initialSize);
            _activeObjects = new HashSet<T>();
            
            // Pre-populate pool
            for (int i = 0; i < initialSize; i++)
            {
                T obj = createFunc();
                obj.OnPoolReturn();
                _pool.Enqueue(obj);
            }
        }
        
        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = _createFunc();
            }
            
            _activeObjects.Add(obj);
            obj.OnPoolGet();
            
            return obj;
        }
        
        public void Return(T obj)
        {
            if (obj == null || !_activeObjects.Contains(obj)) return;
            
            if (!obj.IsAvailableForPool) return;
            
            _activeObjects.Remove(obj);
            obj.OnPoolReturn();
            
            if (_pool.Count < _maxSize)
            {
                _pool.Enqueue(obj);
            }
            else
            {
                // Pool is full, destroy object
                _destroyFunc?.Invoke(obj);
            }
        }
        
        public void Clear()
        {
            // Return all active objects
            var activeObjectsCopy = new List<T>(_activeObjects);
            foreach (var obj in activeObjectsCopy)
            {
                Return(obj);
            }
            
            // Clear remaining pool
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                _destroyFunc?.Invoke(obj);
            }
        }
    }
}