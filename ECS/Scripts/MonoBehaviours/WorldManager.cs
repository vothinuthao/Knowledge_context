using Core.DI;
using Core.ECS;
using Systems.Movement;
using Systems.Squad;
using Systems.Steering;
using UnityEngine;

/// <summary>
/// MonoBehaviour that manages the ECS World and systems
/// </summary>
public class WorldManager : MonoBehaviour
{
    // Singleton instance
    public static WorldManager Instance { get; private set; }
        
    // The ECS World
    private World _world;
        
    // Service container for dependency injection
    private ServiceContainer _serviceContainer;
        
    // Squad command system reference for direct control
    private SquadCommandSystem _squadCommandSystem;
        
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
            
        Instance = this;
        DontDestroyOnLoad(gameObject);
            
        // Initialize world
        _world = new World();
            
        // Initialize dependency injection
        _serviceContainer = new ServiceContainer();
        ServiceLocator.Initialize(_serviceContainer);
            
        // Register services
        RegisterServices();
            
        // Register systems
        RegisterSystems();
    }
        
    /// <summary>
    /// Register services for dependency injection
    /// </summary>
    private void RegisterServices()
    {
        _serviceContainer.RegisterSingleton<World>(_world);
            
        // Register other services...
    }
        
    /// <summary>
    /// Register systems with the world
    /// </summary>
    private void RegisterSystems()
    {
        // Movement systems
        _world.RegisterSystem(new MovementSystem());
        _world.RegisterSystem(new RotationSystem());
            
        // Squad systems
        _world.RegisterSystem(new SquadFormationSystem());
        _squadCommandSystem = _world.RegisterSystem(new SquadCommandSystem());
            
        // Steering systems
        _world.RegisterSystem(new SteeringSystem());
            
        // Register other systems...
    }
        
    private void Update()
    {
        // Update the world
        _world.Update(Time.deltaTime);
    }
        
    /// <summary>
    /// Get the ECS World
    /// </summary>
    public World GetWorld()
    {
        return _world;
    }
        
    /// <summary>
    /// Command a squad to move to a position
    /// </summary>
    public void CommandSquadMove(Entity squadEntity, Vector3 targetPosition)
    {
        _squadCommandSystem.CommandMove(squadEntity, targetPosition);
    }
        
    /// <summary>
    /// Command a squad to attack a target
    /// </summary>
    public void CommandSquadAttack(Entity squadEntity, Entity targetEntity)
    {
        _squadCommandSystem.CommandAttack(squadEntity, targetEntity);
    }
        
    /// <summary>
    /// Command a squad to defend its current position
    /// </summary>
    public void CommandSquadDefend(Entity squadEntity)
    {
        _squadCommandSystem.CommandDefend(squadEntity);
    }
        
    /// <summary>
    /// Command a squad to stop and become idle
    /// </summary>
    public void CommandSquadStop(Entity squadEntity)
    {
        _squadCommandSystem.CommandStop(squadEntity);
    }
}