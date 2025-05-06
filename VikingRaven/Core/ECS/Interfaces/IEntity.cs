namespace VikingRaven.Core.ECS
{
    public interface IEntity
    {
        int Id { get; }
        bool IsActive { get; set; }
        T GetComponent<T>() where T : class, IComponent;
        bool HasComponent<T>() where T : class, IComponent;
        void AddComponent(IComponent component);
        void RemoveComponent<T>() where T : class, IComponent;
    }
}