using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Patterns
{
    /// <summary>
    /// Factory manager template for managing multiple factories
    /// </summary>
    /// <typeparam name="TProduct">Type of product</typeparam>
    /// <typeparam name="TConfig">Type of configuration</typeparam>
    /// <typeparam name="TProductType">Enum type for product variations</typeparam>
    /// <typeparam name="TFactory">Type of factory</typeparam>
    public abstract class FactoryManager<TProduct, TConfig, TProductType, TFactory> : MonoBehaviourSingleton<FactoryManager<TProduct, TConfig, TProductType, TFactory>>
        where TProduct : MonoBehaviour, IFactoryProduct<TConfig>
        where TProductType : Enum
        where TFactory : MonoBehaviourFactory<TProduct, TConfig, TProductType>
    {
        protected Dictionary<TProductType, TFactory> factories = new Dictionary<TProductType, TFactory>();
        
        /// <summary>
        /// Register a factory for a product type
        /// </summary>
        public void RegisterFactory(TProductType productType, TFactory factory)
        {
            if (!factories.ContainsKey(productType))
            {
                factories.Add(productType, factory);
            }
            else
            {
                factories[productType] = factory;
            }
        }
        
        /// <summary>
        /// Get a factory for a product type
        /// </summary>
        public TFactory GetFactory(TProductType productType)
        {
            if (factories.TryGetValue(productType, out TFactory factory))
            {
                return factory;
            }
            
            Debug.LogError($"No factory registered for product type: {productType}");
            return null;
        }
        
        /// <summary>
        /// Create a product using the appropriate factory
        /// </summary>
        public TProduct CreateProduct(TProductType productType, TConfig config, Vector3 position)
        {
            TFactory factory = GetFactory(productType);
            
            if (factory != null)
            {
                return factory.CreateProduct(productType, config, position);
            }
            
            return default;
        }
        
        /// <summary>
        /// Create multiple products using the appropriate factory
        /// </summary>
        public List<TProduct> CreateProducts(TProductType productType, TConfig config, int count, Vector3 centerPosition)
        {
            TFactory factory = GetFactory(productType);
            
            if (factory != null)
            {
                return factory.CreateProducts(productType, config, count, centerPosition);
            }
            
            return new List<TProduct>();
        }
    }
}