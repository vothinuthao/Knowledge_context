using UnityEngine;
using Core.ECS;
using Factories;
using Steering.Config;
using System.Collections.Generic;
using Configs;
using Debug_Tool;
using Management;
using Squad;

/// <summary>
/// GameManager integrates all game systems
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
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
    [SerializeField] private bool showDebugInfo = true;
    
    // Tracking squad and troop entities
    private Dictionary<int, Entity> squads = new Dictionary<int, Entity>();
    private Dictionary<int, GameObject> squadObjects = new Dictionary<int, GameObject>();
    private Dictionary<int, List<Entity>> troopsInSquad = new Dictionary<int, List<Entity>>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Find references if not set
        if (worldManager == null) worldManager = FindObjectOfType<WorldManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (selectionManager == null) selectionManager = FindObjectOfType<SquadSelectionManager>();
        
        if (worldManager == null)
        {
            Debug.LogError("WorldManager không tìm thấy! Hãy đảm bảo đã thêm WorldManager vào scene.");
            enabled = false;
            return;
        }
    }
    
    private void Start()
    {
        // Khởi tạo game
        StartGame();
    }
    
    private void Update()
    {
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
        
        // Toggle debug info với F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            enableTroopDebugging = !enableTroopDebugging;
            showDebugInfo = enableTroopDebugging;
            ToggleTroopDebugging(enableTroopDebugging);
        }
        
        // Refresh squad positions với F2
        if (Input.GetKeyDown(KeyCode.F2) && gridManager != null)
        {
            RefreshSquadPositions();
        }
    }
    
    /// <summary>
    /// Khởi tạo game và tạo test squads
    /// </summary>
    private void StartGame()
    {
        Debug.Log("Khởi tạo game...");
        
        // Wait a frame to ensure everything is initialized
        Invoke("CreateTestSquads", 0.1f);
    }
    
    /// <summary>
    /// Tạo các squad để test
    /// </summary>
    private void CreateTestSquads()
    {
        Debug.Log("Tạo các squad test...");
        
        // Tạo squad đầu tiên
        CreateSquad(new Vector3(3, 0, 3), Quaternion.identity, "Infantry", 9);
        
        // Tạo squad thứ hai
        CreateSquad(new Vector3(9, 0, 9), Quaternion.identity, "Archer", 9);
        
        Debug.Log($"Đã tạo {squads.Count} squads");
        
        // Đảm bảo có layer đúng cho các đối tượng
        EnsureCorrectLayers();
    }
    
    /// <summary>
    /// Tạo một squad mới
    /// </summary>
    public Entity CreateSquad(Vector3 position, Quaternion rotation, string troopType, int troopCount)
    {
        // Kiểm tra vị trí trong grid
        Vector2Int gridCoords = gridManager ? gridManager.GetGridCoordinates(position) : new Vector2Int(0, 0);
        
        // Điều chỉnh vị trí vào grid nếu có thể
        if (gridManager != null)
        {
            if (!gridManager.IsWithinGrid(gridCoords))
            {
                Debug.LogWarning($"Vị trí squad nằm ngoài grid. Điều chỉnh lại vị trí.");
                gridCoords = new Vector2Int(Mathf.Clamp(gridCoords.x, 0, 19), Mathf.Clamp(gridCoords.y, 0, 19));
            }
            
            // Kiểm tra nếu ô đã bị chiếm
            if (gridManager.IsCellOccupied(gridCoords))
            {
                Debug.LogWarning($"Ô ({gridCoords.x}, {gridCoords.y}) đã bị chiếm. Tìm ô trống gần đó.");
                gridCoords = FindNearestEmptyCell(gridCoords);
            }
            
            // Đánh dấu ô là đã bị chiếm
            gridManager.SetCellOccupied(gridCoords, true);
            
            // Lấy tâm của ô
            position = gridManager.GetCellCenter(gridCoords);
        }
        
        // Tạo squad entity
        Entity squadEntity = worldManager.CreateSquad(
            position,
            rotation,
            3, // rows
            3, // columns
            1.5f // spacing
        );
        
        // Tạo visual representation của squad
        GameObject squadObject = new GameObject($"Squad_{squadEntity.Id}");
        squadObject.transform.position = position;
        
        // Đặt layer cho squad object
        squadObject.layer = LayerMask.NameToLayer("Squad");
        
        // Thêm BoxCollider cho squad object
        BoxCollider boxCollider = squadObject.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(5f, 0.5f, 5f);
        boxCollider.center = new Vector3(0, 0.25f, 0);
        boxCollider.isTrigger = true; // Đặt là trigger để không cản trở di chuyển
        
        var squadVisual = squadObject.AddComponent<SquadVisualController>();
        squadVisual.Initialize(squadEntity);
        
        // Lưu trữ references
        int squadId = squadEntity.Id;
        squads[squadId] = squadEntity;
        squadObjects[squadId] = squadObject;
        troopsInSquad[squadId] = new List<Entity>();
        
        // Lấy troop config
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
        
        // Tạo troops
        for (int i = 0; i < troopCount; i++)
        {
            // Tạo troop tại vị trí random gần squad
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            
            Entity troopEntity = worldManager.CreateTroop(
                position + offset,
                rotation,
                troopTypeEnum
            );
            
            // Thêm troop vào squad
            worldManager.AddTroopToSquad(troopEntity, squadEntity);
            
            // Thêm debug component
            if (enableTroopDebugging)
            {
                var entityBehaviour = GetEntityBehaviour(troopEntity);
                if (entityBehaviour != null)
                {
                    // Đảm bảo layer đúng
                    entityBehaviour.gameObject.layer = LayerMask.NameToLayer("Troop");
                    
                    // Thêm CapsuleCollider nếu chưa có
                    Collider troopCollider = entityBehaviour.gameObject.GetComponent<Collider>();
                    if (troopCollider == null)
                    {
                        CapsuleCollider capsuleCollider = entityBehaviour.gameObject.AddComponent<CapsuleCollider>();
                        capsuleCollider.height = 2.0f;
                        capsuleCollider.radius = 0.5f;
                        capsuleCollider.center = new Vector3(0, 1.0f, 0);
                    }
                    // Đăng ký troop visual với squad
                    squadVisual.RegisterTroopVisual(entityBehaviour.transform);
                }
            }
            
            // Apply steering config
            SteeringConfigFactory.ApplyConfig(troopEntity, config.SteeringConfig ?? defaultSteeringConfig);
            
            // Lưu trữ reference
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
        
        for (int i = 0; i < 100; i++) // Giới hạn số lần lặp
        {
            // Kiểm tra ô hiện tại
            if (gridManager.IsWithinGrid(currentCoords) && !gridManager.IsCellOccupied(currentCoords))
            {
                return currentCoords;
            }
            
            // Di chuyển đến ô tiếp theo theo mẫu spiral
            dx += dirX[directionIndex];
            dy += dirY[directionIndex];
            
            currentCoords = new Vector2Int(startCoords.x + dx, startCoords.y + dy);
            
            stepCount++;
            
            // Đổi hướng nếu cần
            if (stepCount == stepSize)
            {
                stepCount = 0;
                directionIndex = (directionIndex + 1) % 4;
                
                // Tăng kích thước bước sau khi hoàn thành nửa spiral
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
        
        // Hiển thị layer debug
        info += "\nLayer Debug:\n";
        info += $"- Ground layer: {LayerMask.NameToLayer("Ground")}\n";
        info += $"- Troop layer: {LayerMask.NameToLayer("Troop")}\n";
        info += $"- Squad layer: {LayerMask.NameToLayer("Squad")}\n";
        
        // Draw info box
        GUI.Box(new Rect(10, 10, 250, 200), "");
        GUI.Label(new Rect(10, 10, 250, 200), info, style);
    }
    
    /// <summary>
    /// Đảm bảo layer được thiết lập đúng cho tất cả đối tượng
    /// </summary>
    private void EnsureCorrectLayers()
    {
        // Kiểm tra các layer
        int groundLayer = LayerMask.NameToLayer("Ground");
        int troopLayer = LayerMask.NameToLayer("Troop");
        int squadLayer = LayerMask.NameToLayer("Squad");
        
        if (groundLayer == -1 || troopLayer == -1 || squadLayer == -1)
        {
            Debug.LogError("Không tìm thấy một hoặc nhiều layer cần thiết (Ground, Troop, Squad). " +
                        "Vui lòng tạo các layer này trong Project Settings.");
            return;
        }
        
        // Thiết lập layer cho Squad và GameObject của chúng
        foreach (var kvp in squadObjects)
        {
            int squadId = kvp.Key;
            GameObject squadObject = kvp.Value;
            
            // Thiết lập layer cho Squad GameObject
            squadObject.layer = squadLayer;
            
            // Thêm BoxCollider nếu chưa có
            Collider squadCollider = squadObject.GetComponent<Collider>();
            if (squadCollider == null)
            {
                BoxCollider boxCollider = squadObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(5f, 0.5f, 5f);
                boxCollider.center = new Vector3(0, 0.25f, 0);
                boxCollider.isTrigger = true;
            }
            
            // Thiết lập layer cho các Troop của Squad này
            if (troopsInSquad.ContainsKey(squadId))
            {
                foreach (var troopEntity in troopsInSquad[squadId])
                {
                    var entityBehaviour = GetEntityBehaviour(troopEntity);
                    if (entityBehaviour != null)
                    {
                        // Thiết lập layer cho Troop GameObject
                        entityBehaviour.gameObject.layer = troopLayer;
                        
                        // Thêm CapsuleCollider nếu chưa có
                        Collider troopCollider = entityBehaviour.gameObject.GetComponent<Collider>();
                        if (troopCollider == null)
                        {
                            CapsuleCollider capsuleCollider = entityBehaviour.gameObject.AddComponent<CapsuleCollider>();
                            capsuleCollider.height = 2.0f;
                            capsuleCollider.radius = 0.5f;
                            capsuleCollider.center = new Vector3(0, 1.0f, 0);
                        }
                    }
                }
            }
        }
        
        // Thiết lập layer cho các ô grid
        if (gridManager != null)
        {
            // Khi grid được tạo, đảm bảo các cell có layer Ground
            // Tạo script giúp đỡ để thay đổi layer
            GameObject helperObject = new GameObject("LayerFixHelper");
            var layerSetupHelper = helperObject.AddComponent<LayerSetupHelper>();
            layerSetupHelper.AssignLayersToObjects();
            
            Debug.Log("Đã thiết lập layer cho tất cả đối tượng");
            
            // Xóa helper object sau khi hoàn thành
            Destroy(helperObject, 1f);
        }
    }
    
    /// <summary>
    /// Bật/tắt debug visualization cho tất cả troop
    /// </summary>
    private void ToggleTroopDebugging(bool enabled)
    {
        enableTroopDebugging = enabled;
        
        foreach (var kvp in troopsInSquad)
        {
            foreach (var troopEntity in kvp.Value)
            {
                var entityBehaviour = GetEntityBehaviour(troopEntity);
                if (entityBehaviour != null)
                {
                    var debugVisualizer = entityBehaviour.GetComponent<TroopDebugVisualizer>();
                    if (debugVisualizer != null)
                    {
                        debugVisualizer.enabled = enabled;
                    }
                }
            }
        }
        
        Debug.Log($"Debug visualization: {(enabled ? "ON" : "OFF")}");
    }
    
    /// <summary>
    /// Cập nhật vị trí của tất cả squad trên grid
    /// </summary>
    private void RefreshSquadPositions()
    {
        if (gridManager == null) return;
        
        // Xóa tất cả ô đã bị chiếm
        gridManager.ClearAllOccupiedCells();
        
        // Đánh dấu lại các ô có squad
        foreach (var kvp in squads)
        {
            Entity squadEntity = kvp.Value;
            
            if (squadEntity.HasComponent<Movement.PositionComponent>())
            {
                Vector3 position = squadEntity.GetComponent<Movement.PositionComponent>().Position;
                Vector2Int cellCoords = gridManager.GetGridCoordinates(position);
                
                if (gridManager.IsWithinGrid(cellCoords))
                {
                    gridManager.SetCellOccupied(cellCoords, true);
                }
            }
        }
        
        Debug.Log("Đã cập nhật vị trí của tất cả squad trên grid");
    }
}