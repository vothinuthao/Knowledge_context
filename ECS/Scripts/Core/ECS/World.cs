using System.Collections.Generic;
using System.Linq;

namespace Core.ECS
{
    /// <summary>
    /// The World manages all entities, components and systems
    /// </summary>
    public class World
    {
        // All entities in the world
        private List<Entity> _entities = new List<Entity>();
        
        // All systems in the world
        private List<ISystem> _systems = new List<ISystem>();
        
        // Registry to track which entities have which components
        private ComponentRegistry _registry = new ComponentRegistry();
        
        // Entity counter for ID generation
        private int _nextEntityId = 0;
        
        /// <summary>
        /// Create a new entity
        /// </summary>
        public Entity CreateEntity()
        {
            var entity = new Entity(_nextEntityId++, this);
            _entities.Add(entity);
            return entity;
        }
        
        /// <summary>
        /// Destroy an entity
        /// </summary>
        public void DestroyEntity(Entity entity)
        {
            _entities.Remove(entity);
            _registry.RemoveEntity(entity);
        }
        
        /// <summary>
        /// Register a system with the world
        /// </summary>
        public T RegisterSystem<T>(T system) where T : ISystem
        {
            _systems.Add(system);
            
            // Sort systems by priority
            _systems = _systems.OrderByDescending(s => s.Priority).ToList();
            
            // Initialize the system
            system.Initialize(this);
            
            return system;
        }
        
        /// <summary>
        /// Update all systems
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var system in _systems)
            {
                system.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// Get entities with specified components
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T>() where T : IComponent
        {
            return _registry.GetEntitiesWith<T>();
        }
        
        /// <summary>
        /// Get entities with multiple component types
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T1, T2>() 
            where T1 : IComponent 
            where T2 : IComponent
        {
            return _registry.GetEntitiesWith<T1, T2>();
        }
        
        /// <summary>
        /// Get entities with multiple component types
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWith<T1, T2, T3>() 
            where T1 : IComponent 
            where T2 : IComponent
            where T3 : IComponent
        {
            return _registry.GetEntitiesWith<T1, T2, T3>();
        }
        
        // Notification methods for component tracking
        internal void NotifyComponentAdded(Entity entity, IComponent component)
        {
            _registry.RegisterComponent(entity, component);
        }
        
        internal void NotifyComponentRemoved(Entity entity, IComponent component)
        {
            _registry.UnregisterComponent(entity, component);
        }
    }
}