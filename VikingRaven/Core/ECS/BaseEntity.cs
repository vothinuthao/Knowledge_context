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
        
        private readonly Dictionary<Type, IComponent> _components = new Dictionary<Type, IComponent>();
        
        public int Id => _id;
        public bool IsActive { get => _isActive; set => _isActive = value; }

        public void Awake()
        {
            // Auto-collect components attached to the GameObject
            foreach (var component in GetComponents<MonoBehaviour>().OfType<IComponent>())
            {
                AddComponent(component);
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