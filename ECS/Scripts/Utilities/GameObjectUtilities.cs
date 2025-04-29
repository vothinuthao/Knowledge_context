using Core.Singleton;
using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Utility class for GameObject operations to centralize instantiation, destruction, and management
    /// </summary>
    public class GameObjectUtilities : Singleton<GameObjectUtilities>
    {
        /// <summary>
        /// Spawn a GameObject from a prefab
        /// </summary>
        /// <param name="prefab">The prefab to spawn from</param>
        /// <param name="position">The position to spawn at</param>
        /// <param name="rotation">The rotation to spawn with</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>The spawned GameObject</returns>
        public GameObject SpawnGameObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot spawn from null prefab!");
                return null;
            }

            GameObject spawnedObject = Object.Instantiate(prefab, position, rotation, parent);
            return spawnedObject;
        }

        /// <summary>
        /// Spawn a GameObject from a prefab and add a component of type T
        /// </summary>
        /// <typeparam name="T">Component type to add</typeparam>
        /// <param name="prefab">The prefab to spawn from</param>
        /// <param name="position">The position to spawn at</param>
        /// <param name="rotation">The rotation to spawn with</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>The added component of type T</returns>
        public T SpawnGameObjectWithComponent<T>(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            GameObject obj = SpawnGameObject(prefab, position, rotation, parent);
            if (obj == null) return null;

            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }

            return component;
        }

        /// <summary>
        /// Safely destroy a GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject to destroy</param>
        /// <param name="delay">Optional delay before destruction</param>
        public void DestroyGameObject(GameObject gameObject, float delay = 0f)
        {
            if (gameObject == null) return;

            Object.Destroy(gameObject, delay);
        }

        /// <summary>
        /// Create a new empty GameObject
        /// </summary>
        /// <param name="name">Name for the new GameObject</param>
        /// <param name="position">Position for the new GameObject</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>The created GameObject</returns>
        public GameObject CreateEmptyGameObject(string name, Vector3 position, Transform parent = null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
        
            if (parent != null)
            {
                obj.transform.SetParent(parent);
            }

            return obj;
        }

        /// <summary>
        /// Find all GameObjects with a specific tag
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        /// <returns>Array of GameObjects with the specified tag</returns>
        public GameObject[] FindGameObjectsWithTag(string tag)
        {
            return GameObject.FindGameObjectsWithTag(tag);
        }

        /// <summary>
        /// Find a GameObject by name
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>The found GameObject or null</returns>
        public GameObject FindGameObjectByName(string name)
        {
            return GameObject.Find(name);
        }

        /// <summary>
        /// Clear all child GameObjects from a transform
        /// </summary>
        /// <param name="parent">The parent transform to clear children from</param>
        public void ClearChildren(Transform parent)
        {
            if (parent == null) return;

            // We need to go backwards through the list because destroying objects changes the count
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Create a primitive shape GameObject
        /// </summary>
        /// <param name="type">Type of primitive to create</param>
        /// <param name="position">Position for the primitive</param>
        /// <param name="color">Color for the primitive</param>
        /// <param name="parent">Optional parent transform</param>
        /// <returns>The created primitive GameObject</returns>
        public GameObject CreatePrimitive(PrimitiveType type, Vector3 position, Color color, Transform parent = null)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.transform.position = position;
        
            if (parent != null)
            {
                primitive.transform.SetParent(parent);
            }

            // Set the color
            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            return primitive;
        }
    }
}