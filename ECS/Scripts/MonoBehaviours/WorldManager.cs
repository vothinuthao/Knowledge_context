using Core.DI;
using Core.ECS;
using Systems.Movement;
using Systems.Squad;
using Systems.Steering;
using System.Collections.Generic;
using Core.Singleton;
using UnityEngine;
using Factories;
using Systems.Combat;

/// <summary>
/// MonoBehaviour that manages the ECS World and systems
/// </summary>
public class WorldManager : ManualSingletonMono<WorldManager>
{
    private World _world;
    // Service container for dependency injection
    private ServiceContainer _serviceContainer;
    
    // System references for direct control
    private SquadCommandSystem _squadCommandSystem;
    
    // Factories
    private TroopFactory _troopFactory;
    
    [SerializeField] 
    private GameObject _troopPrefab; // Assign in inspector
    
    [SerializeField] 
    private bool _debugMode = false; // Enable for debug info
    
    private void Awake()
    {
        base.Awake();
        // Initialize world
        _world = new World();
        
        // Initialize dependency injection
        _serviceContainer = new ServiceContainer();
        ServiceLocator.Initialize(_serviceContainer);
        
        // Register services
        RegisterServices();
        
        // Register systems
        RegisterSystems();
        
        // Initialize factories
        InitializeFactories();
        
        Debug.Log("WorldManager initialized successfully");
    }
    
    /// <summary>
    /// Register services for dependency injection
    /// </summary>
    private void RegisterServices()
    {
        _serviceContainer.RegisterSingleton<World>(_world);
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
        
        // Register steering systems
        RegisterSteeringSystems();
        
        // Combat systems (examples, to be implemented)
        //_world.RegisterSystem(new AttackSystem());
        //_world.RegisterSystem(new DamageSystem());
        
        Debug.Log("All systems registered");
    }
    
    /// <summary>
    /// Register all steering systems
    /// </summary>
    private void RegisterSteeringSystems()
    {
        // Entity detection (must run before steering behaviors)
        _world.RegisterSystem(new EntityDetectionSystem());
        
        // Register all steering systems using the helper
        SteeringSystemRegistry.RegisterSystems(_world);
    }
    
    /// <summary>
    /// Initialize factories
    /// </summary>
    private void InitializeFactories()
    {
        // Create troop factory
        _troopFactory = new TroopFactory(_world, _troopPrefab);
    }
    
    private void Update()
    {
        // Update the world
        _world.Update(Time.deltaTime);
        
        // Debug information
        if (_debugMode)
        {
            DebugWorldInfo();
        }
    }
    
    /// <summary>
    /// Display debug information about the world
    /// </summary>
    private void DebugWorldInfo()
    {
        // Count entities with different component types
        int movementEntities = 0;
        int steeringEntities = 0;
        int squadEntities = 0;
        
        foreach (var entity in _world.GetEntitiesWith<Movement.PositionComponent>())
        {
            movementEntities++;
        }
        
        foreach (var entity in _world.GetEntitiesWith<Steering.SteeringDataComponent>())
        {
            steeringEntities++;
        }
        
        foreach (var entity in _world.GetEntitiesWith<Squad.SquadStateComponent>())
        {
            squadEntities++;
        }
        
        Debug.Log($"World stats: {movementEntities} movement entities, {steeringEntities} steering entities, {squadEntities} squad entities");
    }
    
    /// <summary>
    /// Get the ECS World
    /// </summary>
    public World GetWorld()
    {
        return _world;
    }
    
    /// <summary>
    /// Get the troop factory
    /// </summary>
    public TroopFactory GetTroopFactory()
    {
        return _troopFactory;
    }
    
    /// <summary>
    /// Create a new troop
    /// </summary>
    public Entity CreateTroop(Vector3 position, Quaternion rotation, TroopType troopType)
    {
        return _troopFactory.CreateTroop(position, rotation, troopType);
    }
    
    /// <summary>
    /// Create a new squad
    /// </summary>
    public Entity CreateSquad(Vector3 position, Quaternion rotation, int rows = 3, int columns = 3, float spacing = 1.5f)
    {
        // Create squad entity
        Entity squadEntity = _world.CreateEntity();
        
        // Add position and rotation components
        squadEntity.AddComponent(new Movement.PositionComponent(position));
        squadEntity.AddComponent(new Movement.RotationComponent(rotation, 5.0f));
        
        // Add squad state component
        squadEntity.AddComponent(new Squad.SquadStateComponent());
        
        // Add squad formation component
        squadEntity.AddComponent(new Squad.SquadFormationComponent(rows, columns, spacing));
        
        return squadEntity;
    }
    
    /// <summary>
    /// Add a troop to a squad
    /// </summary>
    public void AddTroopToSquad(Entity troopEntity, Entity squadEntity)
    {
        _troopFactory.AddTroopToSquad(troopEntity, squadEntity);
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