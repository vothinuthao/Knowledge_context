namespace Core.ECS
{
    /// <summary>
    /// Interface for all ECS systems. Systems process entities with specific components.
    /// </summary>
    public interface ISystem
    {
        // Set priority of the system (higher priority systems run first)
        int Priority { get; }
        
        // Initialize the system
        void Initialize(World world);
        
        // Update the system
        void Update(float deltaTime);
    }
}