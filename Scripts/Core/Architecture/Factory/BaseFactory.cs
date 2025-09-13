using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenDeckbuilding.Core.Architecture.Factory
{
    /// <summary>
    /// Generic base factory implementation
    /// </summary>
    public abstract class BaseFactory<TProduct, TData> : MonoBehaviour, IFactory<TProduct, TData>
    {
        [Header("Factory Settings")]
        [SerializeField] protected bool enableLogging = true;
        [SerializeField] protected bool cacheProducts = false;
        
        protected Dictionary<string, TProduct> _productCache;
        protected Dictionary<string, Func<TData, TProduct>> _creators;
        
        protected virtual void Awake()
        {
            if (cacheProducts)
                _productCache = new Dictionary<string, TProduct>();
                
            _creators = new Dictionary<string, Func<TData, TProduct>>();
            RegisterCreators();
        }
        
        /// <summary>
        /// Override to register product creators
        /// </summary>
        protected abstract void RegisterCreators();
        
        /// <summary>
        /// Register a creator function for a specific type
        /// </summary>
        protected void RegisterCreator<T>(string typeName, Func<TData, TProduct> creator) where T : TProduct
        {
            _creators[typeName] = creator;
            
            if (enableLogging)
                Debug.Log($"[{GetType().Name}] Registered creator for: {typeName}");
        }
        
        /// <summary>
        /// Create product from data
        /// </summary>
        public virtual TProduct Create(TData data)
        {
            string typeName = GetTypeName(data);
            
            // Check cache first
            if (cacheProducts && _productCache != null && _productCache.TryGetValue(typeName, out TProduct cachedProduct))
            {
                return cachedProduct;
            }
            
            // Create new product
            if (_creators.TryGetValue(typeName, out Func<TData, TProduct> creator))
            {
                TProduct product = creator(data);
                
                // Cache if enabled
                if (cacheProducts && _productCache != null)
                {
                    _productCache[typeName] = product;
                }
                
                if (enableLogging)
                    Debug.Log($"[{GetType().Name}] Created: {typeName}");
                
                return product;
            }
            
            Debug.LogError($"[{GetType().Name}] No creator found for: {typeName}");
            return default(TProduct);
        }
        
        public virtual bool CanCreate(TData data)
        {
            string typeName = GetTypeName(data);
            return _creators.ContainsKey(typeName);
        }
        
        /// <summary>
        /// Override to extract type name from data
        /// </summary>
        protected abstract string GetTypeName(TData data);
        
        /// <summary>
        /// Clear cache
        /// </summary>
        public virtual void ClearCache()
        {
            _productCache?.Clear();
        }
        
        /// <summary>
        /// Get all registered types
        /// </summary>
        public virtual string[] GetRegisteredTypes()
        {
            var types = new string[_creators.Count];
            _creators.Keys.CopyTo(types, 0);
            return types;
        }
    }
}