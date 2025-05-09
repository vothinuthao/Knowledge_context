using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Core.Factory
{
    public interface IEntityFactory
    {
        IEntity CreateEntity(Vector3 position, Quaternion rotation);
    }
}