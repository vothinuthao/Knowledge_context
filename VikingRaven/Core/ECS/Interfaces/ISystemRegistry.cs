namespace VikingRaven.Core.ECS
{
    public interface ISystemRegistry
    {
        void RegisterSystem(ISystem system);
        void UnregisterSystem(ISystem system);
        void ExecuteAllSystems();
        void InitializeAllSystems();
        void CleanupAllSystems();
    }
}