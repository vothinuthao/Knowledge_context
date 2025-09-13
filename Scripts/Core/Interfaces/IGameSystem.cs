namespace RavenDeckbuilding.Core
{
    /// <summary>
    /// Base interface for all major game systems
    /// </summary>
    public interface IGameSystem
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
        void Update(float deltaTime);
    }
}