// Enhanced World.cs with better entity management
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Performance;
using UnityEngine;

namespace Core.ECS
{
    /// <summary>
    /// Enhanced World class with better performance and entity management
    /// </summary>
    public class World
    {
        // Entity management
        private Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        private Queue<int> _reusableIds = new Queue<int>();
        private int _nextEntityId = 0;
        
        // System management
        private List<ISystem> _systems = new List<ISystem>();
        private Dictionary<Type, ISystem> _systemLookup = new Dictionary<Type, ISystem>();
        
        // Component management - optimized queries
        private Dictionary<Type, HashSet<Entity>> _componentEntities = new Dictionary<Type, HashSet<Entity>>();
        
        // Performance tracking
        private PerformanceMonitor _performanceMonitor;
        
        public World()
        {
            _performanceMonitor = new PerformanceMonitor();
        }
        
        /// <summary>
        /// Create a new entity with optional components
        /// </summary>
        public Entity CreateEntity(params IComponent[] components)
        {
            int id = _reusableIds.Count > 0 ? _reusableIds.Dequeue() : _nextEntityId++;
            var entity = new Entity(id, this);
            _entities[id] = entity;
            
            // Add components if provided
            foreach (var component in components)
            {
                entity.AddComponent(component);
            }
            
            return entity;
        }
        
        /// <summary>
        /// Destroy an entity and release its ID
        /// </summary>
        public void DestroyEntity(Entity entity)
        {
            if (_entities.ContainsKey(entity.Id))
            {
                // Remove from all component lookups
                foreach (var hashSet in _componentEntities.Values)
                {
                    hashSet.Remove(entity);
                }
                
                _entities.Remove(entity.Id);
                _reusableIds.Enqueue(entity.Id);
            }
        }
        
        /// <summary>
        /// Register a system with priority ordering
        /// </summary>
        public T RegisterSystem<T>(T system) where T : ISystem
        {
            Type systemType = typeof(T);
            
            if (_systemLookup.ContainsKey(systemType))
            {
                Debug.LogWarning($"System {systemType.Name} already registered");
                return system;
            }
            
            _systems.Add(system);
            _systemLookup[systemType] = system;
            
            // Re-sort systems by priority
            _systems = _systems.OrderByDescending(s => s.Priority).ToList();
            
            // Initialize system
            system.Initialize(this);
            
            return system;
        }
        
        /// <summary>
        /// Get a registered system by type
        /// </summary>
        public T GetSystem<T>() where T : ISystem
        {
            if (_systemLookup.TryGetValue(typeof(T), out var system))
            {
                return (T)system;
            }
            return default;
        }
        
        /// <summary>
        /// Update all systems with performance tracking
        /// </summary>
        public void Update(float deltaTime)
        {
            _performanceMonitor.StartFrame();
            
            foreach (var system in _systems)
            {
                _performanceMonitor.StartMeasure(system.GetType().Name);
                system.Update(deltaTime);
                _performanceMonitor.EndMeasure(system.GetType().Name);
            }
            
            _performanceMonitor.EndFrame();
        }
        
        /// <summary>
        /// Get entity by ID
        /// </summary>
        public Entity GetEntityById(int id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity : null;
        }
        
        /// <summary>
        /// Optimized query for entities with specific components
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T>() where T : IComponent
        {
            Type componentType = typeof(T);
            
            if (!_componentEntities.ContainsKey(componentType))
            {
                return Enumerable.Empty<Entity>();
            }
            
            return _componentEntities[componentType];
        }
        
        /// <summary>
        /// Query entities with multiple components
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T1, T2>() 
            where T1 : IComponent 
            where T2 : IComponent
        {
            var entities1 = GetEntitiesWith<T1>();
            var entities2 = GetEntitiesWith<T2>();
            return entities1.Intersect(entities2);
        }
        
        /// <summary>
        /// Internal method to notify component addition
        /// </summary>
        internal void NotifyComponentAdded(Entity entity, IComponent component)
        {
            Type componentType = component.GetType();
            
            if (!_componentEntities.ContainsKey(componentType))
            {
                _componentEntities[componentType] = new HashSet<Entity>();
            }
            
            _componentEntities[componentType].Add(entity);
            
            // Also add for interfaces
            foreach (var interfaceType in componentType.GetInterfaces())
            {
                if (typeof(IComponent).IsAssignableFrom(interfaceType))
                {
                    if (!_componentEntities.ContainsKey(interfaceType))
                    {
                        _componentEntities[interfaceType] = new HashSet<Entity>();
                    }
                    _componentEntities[interfaceType].Add(entity);
                }
            }
        }
        
        /// <summary>
        /// Internal method to notify component removal
        /// </summary>
        internal void NotifyComponentRemoved(Entity entity, IComponent component)
        {
            Type componentType = component.GetType();
            
            if (_componentEntities.ContainsKey(componentType))
            {
                _componentEntities[componentType].Remove(entity);
            }
            
            // Also remove for interfaces
            foreach (var interfaceType in componentType.GetInterfaces())
            {
                if (_componentEntities.ContainsKey(interfaceType))
                {
                    _componentEntities[interfaceType].Remove(entity);
                }
            }
        }
        
        /// <summary>
        /// Get performance statistics
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            return _performanceMonitor.GetReport();
        }
    }
}