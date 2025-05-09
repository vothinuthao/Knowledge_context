namespace VikingRaven.Core.ECS
{
    public interface IComponent
    {
        bool IsActive { get; set; }
        IEntity Entity { get; set; }
        void Initialize();
        void Cleanup();
    }
}