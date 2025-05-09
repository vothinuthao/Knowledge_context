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