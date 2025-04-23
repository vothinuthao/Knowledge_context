using System;
using System.Collections.Generic;

namespace Core.ECS
{
    /// <summary>
    /// Represents a game entity with a unique identifier
    /// </summary>
    public class Entity
    {
        // Unique identifier for this entity
        public int Id { get; }
        
        // World reference
        private World _world;
        
        // Dictionary of components indexed by type
        private Dictionary<Type, IComponent> _components = new Dictionary<Type, IComponent>();
        
        public Entity(int id, World world)
        {
            Id = id;
            _world = world;
        }
        
        /// <summary>
        /// Add a component to this entity
        /// </summary>
        public T AddComponent<T>(T component) where T : IComponent
        {
            Type type = typeof(T);
            if (_components.ContainsKey(type))
            {
                throw new InvalidOperationException($"Entity {Id} already has component of type {type.Name}");
            }
            
            _components[type] = component;
            _world.NotifyComponentAdded(this, component);
            return component;
        }
        
        /// <summary>
        /// Get a component of the specified type
        /// </summary>
        public T GetComponent<T>() where T : IComponent
        {
            Type type = typeof(T);
            if (_components.TryGetValue(type, out var component))
            {
                return (T)component;
            }
            return default;
        }
        
        /// <summary>
        /// Check if entity has a component of the specified type
        /// </summary>
        public bool HasComponent<T>() where T : IComponent
        {
            return _components.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Remove a component of the specified type
        /// </summary>
        public void RemoveComponent<T>() where T : IComponent
        {
            Type type = typeof(T);
            if (_components.ContainsKey(type))
            {
                var component = _components[type];
                _components.Remove(type);
                _world.NotifyComponentRemoved(this, component);
            }
        }
    }
}