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
            if (_id > 0 && !_isRegistered)
            {
                if (EntityRegistry.HasInstance)
                {
                    EntityRegistry.Instance.RegisterEntity(this);
                    _isRegistered = true;
                }
            }
            foreach (var component in GetComponents<MonoBehaviour>().OfType<IComponent>())
            {
                AddComponent(component);
            }
        }
        public void SetId(int id)
        {
            if (_id <= 0 || !_isRegistered)
            {
                _id = id;
                if (!_isRegistered && EntityRegistry.HasInstance)
                {
                    EntityRegistry.Instance.RegisterEntity(this);
                    _isRegistered = true;
                }
            }
        }

        public new T GetComponent<T>() where T : class, IComponent
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
            
            if (!_components.TryAdd(type, component))
            {
                return;
            }

            component.Entity = this;
            component.Initialize();
        }

        public void RemoveComponent<T>() where T : class, IComponent
        {
            var type = typeof(T);
            
            if (!_components.TryGetValue(type, out var component))
            {
                return;
            }

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