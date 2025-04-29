using UnityEngine;
using Core.ECS;
using Core.DI;
using Systems;
using Systems.Squad;
using Components;
using Components.Squad;
using System.Collections.Generic;
using Components.Steering;
using Squad;

/// <summary>
/// Initializes the game systems and sets up the scene
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField] private bool createWorldOnStart = true;
    
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private float cellSize = 3.0f;
    
    [Header("Squad Settings")]
    [SerializeField] private bool organizeTroopsOnStart = true;
    [SerializeField] private string troopNamePrefix = "Troop_Warrior_";
    [SerializeField] private int troopsPerSquad = 9;
    
    // Core ECS references
    private World _world;
    private ServiceContainer _serviceContainer;
    
    // System references
    private GridSystem _gridSystem;
    private SelectionSystem _selectionSystem;
    
    private void Awake()
    {
        // Set up DI container
        SetupDependencyInjection();
        
        if (createWorldOnStart)
        {
            CreateECSWorld();
        }
    }
    
    private void Start()
    {
        if (organizeTroopsOnStart)
        {
            OrganizeTroopsIntoSquads();
        }
    }
    
    /// <summary>
    /// Set up the dependency injection container
    /// </summary>
    private void SetupDependencyInjection()
    {
        _serviceContainer = new ServiceContainer();
        ServiceLocator.Initialize(_serviceContainer);
        
        Debug.Log("Dependency injection initialized");
    }
    
    /// <summary>
    /// Create the ECS world and register systems
    /// </summary>
    private void CreateECSWorld()
    {
        // Create world
        _world = new World();
        
        // Register world with service container
        _serviceContainer.RegisterSingleton<World>(_world);
        
        // Register systems
        RegisterSystems();
        
        Debug.Log("ECS World created and systems registered");
    }
    
    /// <summary>
    /// Register all systems with the world
    /// </summary>
    private void RegisterSystems()
    {
        // Register new grid and selection systems
        _gridSystem = _world.RegisterSystem(new GridSystem());
        _selectionSystem = _world.RegisterSystem(new SelectionSystem());
        
        // Register existing systems from base code
        // Keep the original registration order to maintain dependencies
        _world.RegisterSystem(new SquadCommandSystem());
        _world.RegisterSystem(new SquadFormationSystem());
        
        _world.RegisterSystem(new Systems.Movement.GridSquadMovementSystem());
        _world.RegisterSystem(new Systems.Movement.MovementSystem());
        _world.RegisterSystem(new Systems.Movement.RotationSystem());
        
        
        _world.RegisterSystem(new Systems.Behavior.BehaviorSystem());
        _world.RegisterSystem(new Systems.Steering.EntityDetectionSystem());
        _world.RegisterSystem(new Systems.Steering.SteeringSystem());
        _world.RegisterSystem(new Systems.Steering.SeekSystem());
        _world.RegisterSystem(new Systems.Steering.SeparationSystem());
        _world.RegisterSystem(new Systems.Steering.ArrivalSystem());
    }
    
    /// <summary>
    /// Organize troops into squads
    /// </summary>
    private void OrganizeTroopsIntoSquads()
    {
        Debug.Log("Starting to organize troops into squads...");
        
        // Find all GameObjects with the specified prefix
        List<GameObject> allTroops = FindAllTroops();
        if (allTroops.Count == 0)
        {
            Debug.LogWarning("No troops found with prefix: " + troopNamePrefix);
            return;
        }
        
        Debug.Log($"Found {allTroops.Count} troops to organize");
        
        // Group troops by squad based on naming convention
        Dictionary<int, List<GameObject>> squadTroops = GroupTroopsBySquad(allTroops);
        
        foreach (var squadData in squadTroops)
        {
            int squadIndex = squadData.Key;
            List<GameObject> troops = squadData.Value;
            
            CreateSquadFromTroops(squadIndex, troops);
        }
        
        Debug.Log("Finished organizing troops into squads");
    }
    
    /// <summary>
    /// Find all troops in the scene with the specified prefix
    /// </summary>
    private List<GameObject> FindAllTroops()
    {
        List<GameObject> troops = new List<GameObject>();
        
        // Find troops by name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith(troopNamePrefix))
            {
                troops.Add(obj);
                Debug.Log($"Found troop: {obj.name}");
            }
        }
        
        return troops;
    }
    
    /// <summary>
    /// Group troops by squad index based on naming convention
    /// </summary>
    private Dictionary<int, List<GameObject>> GroupTroopsBySquad(List<GameObject> allTroops)
    {
        Dictionary<int, List<GameObject>> squadTroops = new Dictionary<int, List<GameObject>>();
        
        foreach (GameObject troop in allTroops)
        {
            // Parse troop name to get troop index
            int troopIndex = ParseTroopIndex(troop.name);
            int squadIndex = (troopIndex - 1) / troopsPerSquad;
            
            // Create list for this squad if it doesn't exist
            if (!squadTroops.ContainsKey(squadIndex))
            {
                squadTroops[squadIndex] = new List<GameObject>();
            }
            
            // Add troop to the squad
            squadTroops[squadIndex].Add(troop);
        }
        
        return squadTroops;
    }
    
    /// <summary>
    /// Parse troop index from name
    /// </summary>
    private int ParseTroopIndex(string troopName)
    {
        // Extract the numeric part from the end of the name
        string indexStr = troopName.Replace(troopNamePrefix, "");
        int index;
        if (int.TryParse(indexStr, out index))
        {
            return index;
        }
        return 0;
    }
    
    /// <summary>
    /// Create a squad entity and connect troops to it
    /// </summary>
    private void CreateSquadFromTroops(int squadIndex, List<GameObject> troops)
    {
        if (troops.Count == 0) return;
        
        // Calculate the average position for the squad
        Vector3 squadPosition = CalculateAveragePosition(troops);
        
        // Create squad entity
        Entity squadEntity = _world.CreateEntity();
        
        // Add necessary components
        squadEntity.AddComponent(new SquadComponent(troops.Count));
        squadEntity.AddComponent(new PositionComponent(squadPosition));
        squadEntity.AddComponent(new RotationComponent());
        squadEntity.AddComponent(new SquadFormationComponent(3, 3, 1.5f));
        squadEntity.AddComponent(new SelectableComponent());
        
        // Add VelocityComponent for movement
        if (!squadEntity.HasComponent<VelocityComponent>())
        {
            squadEntity.AddComponent(new VelocityComponent(3.5f));
        }
        
        // Create GameObject for the squad
        GameObject squadObject = new GameObject($"Squad_{squadIndex}");
        squadObject.transform.position = squadPosition;
        
        // Create EntityBehaviour component
        EntityBehaviour squadBehavior = squadObject.AddComponent<EntityBehaviour>();
        squadBehavior.Initialize(squadEntity, _world);
        
        // Add a collider for selection
        BoxCollider collider = squadObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(3f, 1f, 3f);
        collider.center = new Vector3(0f, 0.5f, 0f);
        collider.isTrigger = true;
        
        // Add troops to squad
        var squadComponent = squadEntity.GetComponent<SquadComponent>();
        var formationComponent = squadEntity.GetComponent<SquadFormationComponent>();
        
        // Initialize formation
        formationComponent.UpdateWorldPositions(squadPosition, Quaternion.identity);
        
        for (int i = 0; i < troops.Count; i++)
        {
            GameObject troopObject = troops[i];
            
            // Create entity for the troop
            Entity troopEntity = _world.CreateEntity();
            
            // Add components
            troopEntity.AddComponent(new TroopComponent(squadEntity.Id, i));
            troopEntity.AddComponent(new PositionComponent(troopObject.transform.position));
            troopEntity.AddComponent(new RotationComponent(troopObject.transform.rotation, 10f));
            troopEntity.AddComponent(new VelocityComponent(5f));
            
            // Calculate grid position in formation
            int row = i / 3;
            int col = i % 3;
            
            // Add SquadMemberComponent
            troopEntity.AddComponent(new SquadMemberComponent(squadEntity.Id, new Vector2Int(row, col)));
            
            // Add SteeringComponent for movement
            if (!troopEntity.HasComponent<SteeringDataComponent>())
            {
                var steeringComponent = new SteeringDataComponent();
                steeringComponent.TargetPosition = formationComponent.CalculateLocalPosition(row, col) + squadPosition;
                troopEntity.AddComponent(steeringComponent);
            }
            
            // Add EntityBehaviour to GameObject
            EntityBehaviour troopBehavior = troopObject.AddComponent<EntityBehaviour>();
            troopBehavior.Initialize(troopEntity, _world);
            
            // Parent troop to squad
            troopObject.transform.SetParent(squadObject.transform);
            
            // Add to squad
            squadComponent.AddMember(troopEntity.Id);
            formationComponent.SetPositionOccupied(row, col, true);
        }
        
        // Update formation
        squadComponent.UpdateFormation();
        
        Debug.Log($"Created Squad_{squadIndex} with {troops.Count} troops");
    }
    
    /// <summary>
    /// Calculate the average position of a list of GameObjects
    /// </summary>
    private Vector3 CalculateAveragePosition(List<GameObject> objects)
    {
        if (objects.Count == 0) return Vector3.zero;
        
        Vector3 sumPosition = Vector3.zero;
        foreach (GameObject obj in objects)
        {
            sumPosition += obj.transform.position;
        }
        
        return sumPosition / objects.Count;
    }
}