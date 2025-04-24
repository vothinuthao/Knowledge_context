// File: ECS/Scripts/Core/ECS/ComponentRegistry.cs (Updated)
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.ECS
{
    /// <summary>
    /// Tracks which entities have which components for efficient querying
    /// </summary>
    internal class ComponentRegistry
    {
        // Map from component type to set of entities with that component
        private Dictionary<Type, HashSet<Entity>> _componentMap = new Dictionary<Type, HashSet<Entity>>();
        
        /// <summary>
        /// Register a component for an entity
        /// </summary>
        public void RegisterComponent(Entity entity, IComponent component)
        {
            Type type = component.GetType();
            
            if (!_componentMap.TryGetValue(type, out var entities))
            {
                entities = new HashSet<Entity>();
                _componentMap[type] = entities;
            }
            
            entities.Add(entity);
            
            // Also register for interface types
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType != typeof(IComponent) && typeof(IComponent).IsAssignableFrom(interfaceType))
                {
                    if (!_componentMap.TryGetValue(interfaceType, out var interfaceEntities))
                    {
                        interfaceEntities = new HashSet<Entity>();
                        _componentMap[interfaceType] = interfaceEntities;
                    }
                    
                    interfaceEntities.Add(entity);
                }
            }
        }
        
        /// <summary>
        /// Unregister a component for an entity
        /// </summary>
        public void UnregisterComponent(Entity entity, IComponent component)
        {
            Type type = component.GetType();
            
            if (_componentMap.TryGetValue(type, out var entities))
            {
                entities.Remove(entity);
            }
            
            // Also unregister for interface types
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType != typeof(IComponent) && typeof(IComponent).IsAssignableFrom(interfaceType))
                {
                    if (_componentMap.TryGetValue(interfaceType, out var interfaceEntities))
                    {
                        interfaceEntities.Remove(entity);
                    }
                }
            }
        }
        
        /// <summary>
        /// Remove all component registrations for an entity
        /// </summary>
        public void RemoveEntity(Entity entity)
        {
            foreach (var entities in _componentMap.Values)
            {
                entities.Remove(entity);
            }
        }
        
        /// <summary>
        /// Get all entities with a specific component type
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T>() where T : IComponent
        {
            Type type = typeof(T);
            
            if (_componentMap.TryGetValue(type, out var entities))
            {
                return entities;
            }
            
            return Enumerable.Empty<Entity>();
        }
        
        /// <summary>
        /// Get all entities with two component types
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T1, T2>() 
            where T1 : IComponent 
            where T2 : IComponent
        {
            return GetEntitiesWith<T1>().Intersect(GetEntitiesWith<T2>());
        }
        
        /// <summary>
        /// Get all entities with three component types
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3>() 
            where T1 : IComponent 
            where T2 : IComponent
            where T3 : IComponent
        {
            return GetEntitiesWith<T1, T2>().Intersect(GetEntitiesWith<T3>());
        }
        
        /// <summary>
        /// Get all entities with four component types
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3, T4>() 
            where T1 : IComponent 
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent
        {
            return GetEntitiesWith<T1, T2, T3>().Intersect(GetEntitiesWith<T4>());
        }
    }
}