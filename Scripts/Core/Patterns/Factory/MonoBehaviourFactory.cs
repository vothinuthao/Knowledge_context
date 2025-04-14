using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Patterns
{
    /// <summary>
    /// Abstract base class for MonoBehaviour factories
    /// </summary>
    /// <typeparam name="TProduct">Type of product to create</typeparam>
    /// <typeparam name="TConfig">Type of configuration to use</typeparam>
    /// <typeparam name="TProductType">Enum type for product variations</typeparam>
    public abstract class MonoBehaviourFactory<TProduct, TConfig, TProductType> : MonoBehaviour, IFactory<TProduct, TConfig, TProductType> 
        where TProduct : MonoBehaviour, IFactoryProduct<TConfig> 
        where TProductType : Enum
    {
        [SerializeField] protected Transform productsParent;
        
        /// <summary>
        /// Create a prefab dictionary mapping product types to prefabs
        /// </summary>
        protected abstract Dictionary<TProductType, GameObject> CreatePrefabDictionary();
        
        /// <summary>
        /// Get the prefab for a specific product type
        /// </summary>
        protected GameObject GetPrefab(TProductType productType)
        {
            var prefabDictionary = CreatePrefabDictionary();
            
            if (prefabDictionary.TryGetValue(productType, out GameObject prefab))
            {
                return prefab;
            }
            
            Debug.LogError($"No prefab found for product type: {productType}");
            return null;
        }
        
        /// <summary>
        /// Create a product of specified type
        /// </summary>
        public virtual TProduct CreateProduct(TProductType productType, TConfig config, Vector3 position)
        {
            GameObject prefab = GetPrefab(productType);
            
            if (prefab == null)
                return default;
            
            // Instantiate the prefab
            GameObject instance;
            if (productsParent != null)
            {
                instance = Instantiate(prefab, position, Quaternion.identity, productsParent);
            }
            else
            {
                instance = Instantiate(prefab, position, Quaternion.identity);
            }
            
            // Get the product component
            TProduct product = instance.GetComponent<TProduct>();
            
            if (product == null)
            {
                Debug.LogError($"Prefab does not have a component of type {typeof(TProduct).Name}");
                Destroy(instance);
                return default;
            }
            
            // Initialize the product
            product.Initialize(config);
            
            // Additional initialization if needed
            OnProductCreated(product, productType, config);
            
            return product;
        }
        
        /// <summary>
        /// Create multiple products of specified type
        /// </summary>
        public virtual List<TProduct> CreateProducts(TProductType productType, TConfig config, int count, Vector3 centerPosition)
        {
            List<TProduct> products = new List<TProduct>();
            
            // Create parent for the group
            GameObject groupParent = new GameObject($"{productType}_Group");
            groupParent.transform.position = centerPosition;
            
            if (productsParent != null)
            {
                groupParent.transform.parent = productsParent;
            }
            
            // Get positions for the products
            Vector3[] positions = CalculatePositions(count, centerPosition);
            
            // Create each product
            for (int i = 0; i < count; i++)
            {
                TProduct product = CreateProduct(productType, config, positions[i]);
                
                if (product != null)
                {
                    // Set parent to group
                    product.transform.parent = groupParent.transform;
                    
                    // Add to list
                    products.Add(product);
                }
            }
            
            return products;
        }
        
        /// <summary>
        /// Called when a product is created
        /// Override to add custom initialization
        /// </summary>
        protected virtual void OnProductCreated(TProduct product, TProductType productType, TConfig config) { }
        
        /// <summary>
        /// Calculate positions for multiple products
        /// Override to customize positioning logic
        /// </summary>
        protected virtual Vector3[] CalculatePositions(int count, Vector3 centerPosition)
        {
            Vector3[] positions = new Vector3[count];
            
            // Default implementation: grid layout
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));
            float spacing = 1.0f;
            
            for (int i = 0; i < count; i++)
            {
                int row = i / gridSize;
                int col = i % gridSize;
                
                float x = (col - (gridSize - 1) / 2.0f) * spacing;
                float z = (row - (gridSize - 1) / 2.0f) * spacing;
                
                positions[i] = centerPosition + new Vector3(x, 0, z);
            }
            
            return positions;
        }
    }
}