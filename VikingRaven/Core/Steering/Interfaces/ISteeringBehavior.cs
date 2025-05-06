using VikingRaven.Core.ECS;

namespace VikingRaven.Core.Steering
{
    public interface ISteeringBehavior
    {
        string Name { get; }
        float Weight { get; set; }
        bool IsActive { get; set; }
        
        SteeringOutput Calculate(IEntity entity);
    }
}