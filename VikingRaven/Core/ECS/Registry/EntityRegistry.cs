﻿using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using UnityEngine;

namespace VikingRaven.Core.ECS
{
    public class EntityRegistry : Singleton<EntityRegistry>, IEntityRegistry
    {
        private readonly Dictionary<int, IEntity> _entities = new Dictionary<int, IEntity>();
        private int _nextEntityId = 1;
        
        public void RegisterEntity(IEntity entity)
        {
            if (entity is { Id: > 0 })
            {
                _entities.TryAdd(entity.Id, entity);
            }
        }
        public int EntityCount => _entities.Count;
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
            var fieldInfo = typeof(BaseEntity).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null) fieldInfo.SetValue(entity, _nextEntityId);

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
                var obj = (entity as MonoBehaviour)?.gameObject;
                if (obj != null)
                {
                    Destroy(obj);
                }
                
                _entities.Remove(id);
            }
        }
        public void DestroyAllEntities()
        {
            foreach (var entity in _entities.Values)
            {
                var obj = (entity as MonoBehaviour)?.gameObject;
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            _entities.Clear();
        }
    }
}