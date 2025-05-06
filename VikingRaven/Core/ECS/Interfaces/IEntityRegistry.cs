using System.Collections.Generic;

namespace VikingRaven.Core.ECS
{
    public interface IEntityRegistry
    {
        IEntity CreateEntity();
        IEntity GetEntity(int id);
        List<IEntity> GetAllEntities();
        List<IEntity> GetEntitiesWithComponent<T>() where T : class, IComponent;
        void DestroyEntity(int id);
        void DestroyAllEntities();
    }
}