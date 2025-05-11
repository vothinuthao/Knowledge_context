using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VikingRaven.Core.ECS
{
    public class BaseEntity : MonoBehaviour, IEntity
    {
        [SerializeField] private int _id;
        [SerializeField] private bool _isActive = true;
        [SerializeField] private bool _isRegistered = false;
        
        private readonly Dictionary<Type, IComponent> _components = new Dictionary<Type, IComponent>();
        
        public int Id => _id;
        public bool IsActive { get => _isActive; set => _isActive = value; }

        public void Awake()
        {
            // Only try to register if we have a valid ID and are not already registered
            if (_id > 0 && !_isRegistered)
            {
                if (EntityRegistry.HasInstance)
                {
                    EntityRegistry.Instance.RegisterEntity(this);
                    _isRegistered = true;
                    Debug.Log($"BaseEntity: Awake with ID {_id}, registering with EntityRegistry");
                }
                else
                {
                    Debug.LogError($"BaseEntity: EntityRegistry instance not available, cannot register entity {_id}");
                }
            }
            else if (_id <= 0)
            {
                Debug.LogWarning($"BaseEntity: Entity has invalid ID {_id}, skipping automatic registration");
            }
            
            // Add all components from GameObject
            foreach (var component in GetComponents<MonoBehaviour>().OfType<IComponent>())
            {
                AddComponent(component);
            }
        }

        // Allow setting ID from outside (e.g. from factory)
        public void SetId(int id)
        {
            if (_id <= 0 || !_isRegistered)
            {
                _id = id;
                
                // Register with EntityRegistry if not already registered
                if (!_isRegistered && EntityRegistry.HasInstance)
                {
                    EntityRegistry.Instance.RegisterEntity(this);
                    _isRegistered = true;
                    Debug.Log($"BaseEntity: ID set to {_id}, registered with EntityRegistry");
                }
            }
            else
            {
                Debug.LogWarning($"BaseEntity: Attempt to change ID from {_id} to {id} ignored, entity already registered");
            }
        }

        public T GetComponent<T>() where T : class, IComponent
        {
            if (_components.TryGetValue(typeof(T), out var component))
            {
                return component as T;
            }
            return null;
        }

        public T GetComponentBehavior<T>() where T : class
        {
            if (_components.TryGetValue(typeof(T), out var component))
            {
                return component as T;
            }
            return null;
        }

        public bool HasComponent<T>() where T : class, IComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        public void AddComponent(IComponent component)
        {
            var type = component.GetType();
            
            if (_components.ContainsKey(type))
            {
                Debug.LogWarning($"Entity {Id} already has component of type {type.Name}");
                return;
            }
            
            _components[type] = component;
            component.Entity = this;
            component.Initialize();
        }

        public void RemoveComponent<T>() where T : class, IComponent
        {
            var type = typeof(T);
            
            if (!_components.ContainsKey(type))
            {
                Debug.LogWarning($"Entity {Id} doesn't have component of type {type.Name} to remove");
                return;
            }
            
            var component = _components[type];
            component.Cleanup();
            component.Entity = null;
            _components.Remove(type);
        }

        public void OnDestroy()
        {
            foreach (var component in _components.Values)
            {
                component.Cleanup();
            }
            _components.Clear();
        }
    }
}