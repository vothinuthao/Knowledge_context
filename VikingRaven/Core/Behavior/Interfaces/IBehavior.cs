using VikingRaven.Core.ECS;

namespace VikingRaven.Core.Behavior
{
    public interface IBehavior
    {
        string Name { get; }
        float Weight { get; }
        bool IsActive { get; set; }
        IEntity Entity { get; }
        
        float CalculateWeight();
        void Execute();
        void Initialize();
        void Cleanup();
    }
}