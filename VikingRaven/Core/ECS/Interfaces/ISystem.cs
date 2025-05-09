namespace VikingRaven.Core.ECS
{
    public interface ISystem
    {
        int Priority { get; }
        bool IsActive { get; set; }
        void Initialize();
        void Execute();
        void Cleanup();
    }
}