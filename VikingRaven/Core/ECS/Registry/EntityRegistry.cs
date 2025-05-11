using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using UnityEngine;

namespace VikingRaven.Core.ECS
{
    public class EntityRegistry : Singleton<EntityRegistry>, IEntityRegistry
    {
        private readonly Dictionary<int, IEntity> _entities = new Dictionary<int, IEntity>();
        private int _nextEntityId = 1;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("EntityRegistry initialized as singleton");
        }

        // New method to register entities from outside
        public void RegisterEntity(IEntity entity)
        {
            if (entity is { Id: > 0 })
            {
                if (!_entities.ContainsKey(entity.Id))
                {
                    _entities[entity.Id] = entity;
                    Debug.Log($"EntityRegistry: Registered entity with ID {entity.Id}");
                }
                else
                {
                    // Entity already registered, skip
                    Debug.LogWarning($"EntityRegistry: Entity with ID {entity.Id} already registered");
                }
            }
            else
            {
                Debug.LogError("EntityRegistry: Cannot register null entity or entity with invalid ID");
            }
        }

        // Added for debugging
        public int EntityCount => _entities.Count;

        // Added for debugging
        public void LogAllEntities()
        {
            Debug.Log($"EntityRegistry: Contains {_entities.Count} entities");
            foreach (var entity in _entities.Values)
            {
                Debug.Log($"EntityRegistry: Entity {entity.Id}, IsActive = {entity.IsActive}");
            }
        }

        public IEntity CreateEntity()
        {
            var entityObject = new GameObject($"Entity_{_nextEntityId}");
            var entity = entityObject.AddComponent<BaseEntity>();
            
            // Use reflection to set the ID field
            var fieldInfo = typeof(BaseEntity).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo.SetValue(entity, _nextEntityId);
            
            _entities[_nextEntityId] = entity;
            _nextEntityId++;
            
            return entity;
        }

        public IEntity GetEntity(int id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity : null;
        }

        public List<IEntity> GetAllEntities()
        {
            return _entities.Values.Where(e => e.IsActive).ToList();
        }

        public List<IEntity> GetEntitiesWithComponent<T>() where T : class, IComponent
        {
            return _entities.Values
                .Where(e => e.IsActive && e.HasComponent<T>())
                .ToList();
        }

        public void DestroyEntity(int id)
        {
            if (_entities.TryGetValue(id, out var entity))
            {
                var gameObject = (entity as MonoBehaviour)?.gameObject;
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
                
                _entities.Remove(id);
            }
        }

        public void DestroyAllEntities()
        {
            foreach (var entity in _entities.Values)
            {
                var gameObject = (entity as MonoBehaviour)?.gameObject;
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            }
            
            _entities.Clear();
        }
    }
}