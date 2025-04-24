using UnityEngine;
using Core.ECS;
using Factories;
using Steering.Config;
using System.Collections.Generic;
using Configs;
using Core.Singleton;
using Debug_Tool;
using Management;
using Squad;

/// <summary>
/// GameManager tích hợp tất cả các hệ thống của game
/// </summary>
public class GameManager : ManualSingletonMono<GameManager>
{
    
    [Header("References")]
    [SerializeField] private WorldManager worldManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private SquadSelectionManager selectionManager;
    [SerializeField] private GameObject squadFlagPrefab;
    
    [Header("Configuration")]
    [SerializeField] private TroopConfigSO[] troopConfigs;
    [SerializeField] private SteeringBehaviorConfig defaultSteeringConfig;
    
    [Header("Debug")]
    [SerializeField] private bool enableTroopDebugging = true;
    [SerializeField] private bool showDebugInfo = false;
    
    // Tracking squad and troop entities
    private Dictionary<int, Entity> squads = new Dictionary<int, Entity>();
    private Dictionary<int, GameObject> squadObjects = new Dictionary<int, GameObject>();
    private Dictionary<int, List<Entity>> troopsInSquad = new Dictionary<int, List<Entity>>();

    protected override void Awake()
    {
        base.Awake();
    }
    
    private void Start()
    {
        StartGame();
    }
    
    private void Update()
    {
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }
    
    /// <summary>
    /// Khởi tạo game và tạo test squads
    /// </summary>
    private void StartGame()
    {
        Debug.Log("Khởi tạo game...");
        Invoke(nameof(CreateTestSquads), 0.1f);
    }
    
    /// <summary>
    /// Tạo các squad để test
    /// </summary>
    private void CreateTestSquads()
    {
        Debug.Log("Tạo các squad test...");
        CreateSquad(new Vector3(3, 0, 3), Quaternion.identity, "Infantry", 9);
        CreateSquad(new Vector3(9, 0, 9), Quaternion.identity, "Archer", 9);
        Debug.Log($"Đã tạo {squads.Count} squads");
    }
    
    /// <summary>
    /// Tạo một squad mới
    /// </summary>
    public Entity CreateSquad(Vector3 position, Quaternion rotation, string troopType, int troopCount)
    {
        // Check if position is within grid
        Vector2Int gridCoords = gridManager ? gridManager.GetGridCoordinates(position) : new Vector2Int(0, 0);
        
        // Adjust position to grid if applicable
        if (gridManager != null)
        {
            if (!gridManager.IsWithinGrid(gridCoords))
            {
                Debug.LogWarning($"Vị trí squad nằm ngoài grid. Điều chỉnh lại vị trí.");
                gridCoords = new Vector2Int(Mathf.Clamp(gridCoords.x, 0, 19), Mathf.Clamp(gridCoords.y, 0, 19));
            }
            
            // Check if cell is occupied
            if (gridManager.IsCellOccupied(gridCoords))
            {
                Debug.LogWarning($"Ô ({gridCoords.x}, {gridCoords.y}) đã bị chiếm. Tìm ô trống gần đó.");
                gridCoords = FindNearestEmptyCell(gridCoords);
            }
            gridManager.SetCellOccupied(gridCoords, true);
            position = gridManager.GetCellCenter(gridCoords);
        }
        Entity squadEntity = worldManager.CreateSquad(
            position,
            rotation,
            3, // rows
            3, // columns
            1.5f // spacing
        );
        
        // Create visual representation of squad
        GameObject squadObject = new GameObject($"Squad_{squadEntity.Id}");
        squadObject.transform.position = position;
        
        var squadVisual = squadObject.AddComponent<SquadVisualController>();
        squadVisual.Initialize(squadEntity);
        
        // Store references
        int squadId = squadEntity.Id;
        squads[squadId] = squadEntity;
        squadObjects[squadId] = squadObject;
        troopsInSquad[squadId] = new List<Entity>();
        
        // Get troop config
        TroopConfigSO config = GetTroopConfig(troopType);
        
        if (config == null)
        {
            Debug.LogError($"Không tìm thấy config cho troop type: {troopType}. Sử dụng config mặc định.");
            config = troopConfigs.Length > 0 ? troopConfigs[0] : null;
        }
        
        if (config == null)
        {
            Debug.LogError("Không có troop config nào. Không thể tạo troop.");
            return squadEntity;
        }
        
        // Get troop type enum
        TroopType troopTypeEnum = GetTroopTypeEnum(troopType);
        
        // Create troops
        for (int i = 0; i < troopCount; i++)
        {
            // Create troop at random position near squad
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            
            Entity troopEntity = worldManager.CreateTroop(
                position + offset,
                rotation,
                troopTypeEnum
            );
            
            // Add troop to squad
            worldManager.AddTroopToSquad(troopEntity, squadEntity);
            
            // Add debug component
            if (enableTroopDebugging)
            {
                var entityBehaviour = GetEntityBehaviour(troopEntity);
                if (entityBehaviour != null)
                {
                    entityBehaviour.gameObject.AddComponent<TroopDebugVisualizer>();
                    
                    // Register troop visual to squad
                    squadVisual.RegisterTroopVisual(entityBehaviour.transform);
                }
            }
            
            // Apply steering config
            SteeringConfigFactory.ApplyConfig(troopEntity, config.SteeringConfig ?? defaultSteeringConfig);
            
            // Store reference
            troopsInSquad[squadId].Add(troopEntity);
        }
        
        Debug.Log($"Tạo squad {squadId} với {troopCount} {troopType} troops tại {position}");
        
        return squadEntity;
    }
    
    /// <summary>
    /// Tìm ô trống gần nhất
    /// </summary>
    private Vector2Int FindNearestEmptyCell(Vector2Int startCoords)
    {
        if (gridManager == null)
            return startCoords;
            
        // Spiral search pattern
        int dx = 0;
        int dy = 0;
        int stepSize = 1;
        int stepCount = 0;
        int directionIndex = 0;
        
        // Directions: right, down, left, up
        int[] dirX = { 1, 0, -1, 0 };
        int[] dirY = { 0, 1, 0, -1 };
        
        Vector2Int currentCoords = startCoords;
        
        for (int i = 0; i < 100; i++) // Limit iterations
        {
            // Check current cell
            if (gridManager.IsWithinGrid(currentCoords) && !gridManager.IsCellOccupied(currentCoords))
            {
                return currentCoords;
            }
            
            // Move to next cell in spiral
            dx += dirX[directionIndex];
            dy += dirY[directionIndex];
            
            currentCoords = new Vector2Int(startCoords.x + dx, startCoords.y + dy);
            
            stepCount++;
            
            // Change direction if needed
            if (stepCount == stepSize)
            {
                stepCount = 0;
                directionIndex = (directionIndex + 1) % 4;
                
                // Increase step size after completing half of the spiral
                if (directionIndex % 2 == 0)
                {
                    stepSize++;
                }
            }
        }
        
        // Fallback to original position if nothing found
        return startCoords;
    }
    
    /// <summary>
    /// Lấy config cho loại troop
    /// </summary>
    private TroopConfigSO GetTroopConfig(string troopName)
    {
        foreach (var config in troopConfigs)
        {
            if (config.TroopName == troopName)
                return config;
        }
        return null;
    }
    
    /// <summary>
    /// Chuyển đổi từ string sang enum TroopType
    /// </summary>
    private TroopType GetTroopTypeEnum(string troopType)
    {
        switch (troopType.ToLower())
        {
            case "infantry": return TroopType.Warrior;
            case "heavyinfantry": return TroopType.HeavyInfantry;
            case "berserker": return TroopType.Berserker;
            case "archer": return TroopType.Archer;
            case "scout": return TroopType.Scout;
            case "commander": return TroopType.Commander;
            case "defender": return TroopType.Defender;
            case "assassin": return TroopType.Assassin;
            default: return TroopType.Warrior;
        }
    }
    
    /// <summary>
    /// Lấy EntityBehaviour cho entity
    /// </summary>
    private EntityBehaviour GetEntityBehaviour(Entity entity)
    {
        if (entity == null)
            return null;
            
        EntityBehaviour[] allEntityBehaviours = FindObjectsOfType<EntityBehaviour>();
        
        foreach (var behaviour in allEntityBehaviours)
        {
            if (behaviour.GetEntity()?.Id == entity.Id)
            {
                return behaviour;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Hiển thị thông tin debug trên màn hình
    /// </summary>
    private void DrawDebugInfo()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        style.padding = new RectOffset(10, 10, 10, 10);
        
        string info = $"Squads: {squads.Count}\n";
        
        int selectedSquadId = -1;
        var selectedSquad = selectionManager != null ? selectionManager.GetSelectedSquad() : null;
        
        if (selectedSquad != null)
        {
            selectedSquadId = selectedSquad.Id;
            info += $"Selected: Squad {selectedSquadId}\n";
            
            if (selectedSquad.HasComponent<SquadStateComponent>())
            {
                var state = selectedSquad.GetComponent<SquadStateComponent>();
                info += $"State: {state.CurrentState}\n";
                info += $"Target: {state.TargetPosition}\n";
            }
            
            if (troopsInSquad.ContainsKey(selectedSquadId))
            {
                info += $"Troops: {troopsInSquad[selectedSquadId].Count}\n";
            }
        }
        else
        {
            info += "No squad selected\n";
        }
        
        // Draw info box
        GUI.Box(new Rect(10, 10, 200, 100), "");
        GUI.Label(new Rect(10, 10, 200, 100), info, style);
    }
}