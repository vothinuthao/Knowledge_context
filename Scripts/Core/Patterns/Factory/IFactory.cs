// Assets/Scripts/Core/Patterns/GenericFactory.cs

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Patterns
{
    /// <summary>
    /// Interface for factory product
    /// </summary>
    /// <typeparam name="TProduct">Type of the product</typeparam>
    /// <typeparam name="TConfig">Type of the configuration</typeparam>
    public interface IFactoryProduct<TConfig>
    {
        /// <summary>
        /// Initialize the product with configuration
        /// </summary>
        void Initialize(TConfig config);
    }

    /// <summary>
    /// Interface for generic factory
    /// </summary>
    /// <typeparam name="TProduct">Type of product to create</typeparam>
    /// <typeparam name="TConfig">Type of configuration to use</typeparam>
    /// <typeparam name="TProductType">Enum type for product variations</typeparam>
    public interface IFactory<TProduct, TConfig, TProductType> where TProduct : IFactoryProduct<TConfig> where TProductType : Enum
    {
        /// <summary>
        /// Create a product of specified type
        /// </summary>
        TProduct CreateProduct(TProductType productType, TConfig config, Vector3 position);
        
        /// <summary>
        /// Create multiple products of specified type
        /// </summary>
        List<TProduct> CreateProducts(TProductType productType, TConfig config, int count, Vector3 centerPosition);
    }

}